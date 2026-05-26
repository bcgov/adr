namespace Adr.PublicBodies.Services
{
    using System.Collections.Generic;
    using System.Linq;
    using Adr.PublicBodies.Models;
    using Adr.PublicBodies.Providers;
    using Microsoft.Extensions.Logging;
    using System;

    public class PublicBodyService : IPublicBodyService
    {
        private readonly ILogger<PublicBodyService> _logger;
        private readonly IPublicBodyProvider _publicBodyProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="PublicBodyService"/> class.
        /// </summary>
        /// <param name="logger">Injected Logger Provider.</param>
        public PublicBodyService(
            ILogger<PublicBodyService> logger,
            IPublicBodyProvider publicBodyProvider
        )
        {
            _logger = logger;
            _publicBodyProvider = publicBodyProvider;
        }

        /// <inheritdoc/>
        public IEnumerable<PublicBodyModel> GetAll()
        {
            return _publicBodyProvider.GetAllPublicBodies();
        }

        /// <inheritdoc/>
        public PublicBodyModel? GetPublicBody(string id)
        {
            var publicBodies = _publicBodyProvider.GetAllPublicBodies();
            return publicBodies.FirstOrDefault(x => x.StaticId == id);
        }

        /// <inheritdoc/>
        IEnumerable<PublicBodyTypeModel> IPublicBodyService.GetAllTypes()
        {
            return _publicBodyProvider.GetAllTypes();
        }

        /// <inheritdoc/>
        public IEnumerable<PublicBodyParentChildModel> GetAllParentChildRelationships()
        {
            var bodies = _publicBodyProvider.GetAllPublicBodies().ToList();
            var relationships = _publicBodyProvider.GetAllParentChildRelationships().ToList();
            PopulateRelationshipFlags(relationships, bodies);
            return relationships;
        }

        /// <inheritdoc/>
        public PublicBodyHistoryModel? GetHistory(string id)
        {
            var allBodies = _publicBodyProvider.GetAllPublicBodies().ToList();
            var allRelationships = _publicBodyProvider.GetAllParentChildRelationships().ToList();
            PopulateRelationshipFlags(allRelationships, allBodies);

            var startBody = allBodies.FirstOrDefault(b => b.StaticId == id);
            if (startBody == null)
            {
                return null;
            }

            var visitedNodeIds = new HashSet<string>();
            var collectedEdges = new List<PublicBodyParentChildModel>();

            visitedNodeIds.Add(id);

            // Walk descendants (forward through children)
            var descendantQueue = new Queue<string>();
            descendantQueue.Enqueue(id);
            while (descendantQueue.Count > 0)
            {
                var currentId = descendantQueue.Dequeue();
                var childEdges = allRelationships.Where(r => r.ParentUniqueId == currentId);
                foreach (var edge in childEdges)
                {
                    collectedEdges.Add(edge);
                    if (visitedNodeIds.Add(edge.ChildUniqueId))
                    {
                        descendantQueue.Enqueue(edge.ChildUniqueId);
                    }
                }
            }

            // Walk ancestors (backward through parents)
            var ancestorQueue = new Queue<string>();
            ancestorQueue.Enqueue(id);
            while (ancestorQueue.Count > 0)
            {
                var currentId = ancestorQueue.Dequeue();
                var parentEdges = allRelationships.Where(r => r.ChildUniqueId == currentId);
                foreach (var edge in parentEdges)
                {
                    collectedEdges.Add(edge);
                    if (visitedNodeIds.Add(edge.ParentUniqueId))
                    {
                        ancestorQueue.Enqueue(edge.ParentUniqueId);
                    }
                }
            }

            var nodes = allBodies.Where(b => visitedNodeIds.Contains(b.StaticId));

            return new PublicBodyHistoryModel
            {
                PublicBodyId = id,
                PublicBodies = nodes,
                Relationships = collectedEdges,
            };
        }

        // Computes WasSplit / WasMerged / WasRenamed for each relationship from
        // the global graph topology and the bodies they connect:
        //   - WasSplit:   parent has more than one outgoing edge
        //   - WasMerged:  child has more than one incoming edge
        //   - WasRenamed: parent and child share a non-empty PublicBodyId
        //                 (i.e. they are two records of the same logical body)
        private static void PopulateRelationshipFlags(
            List<PublicBodyParentChildModel> relationships,
            List<PublicBodyModel> bodies
        )
        {
            var bodiesById = bodies.ToDictionary(b => b.StaticId, b => b);

            var childCountByParent = relationships
                .GroupBy(r => r.ParentUniqueId)
                .ToDictionary(g => g.Key, g => g.Count());

            var parentCountByChild = relationships
                .GroupBy(r => r.ChildUniqueId)
                .ToDictionary(g => g.Key, g => g.Count());

            foreach (var rel in relationships)
            {
                rel.WasSplit =
                    childCountByParent.TryGetValue(rel.ParentUniqueId, out var childCount)
                    && childCount > 1;

                rel.WasMerged =
                    parentCountByChild.TryGetValue(rel.ChildUniqueId, out var parentCount)
                    && parentCount > 1;

                rel.WasRenamed =
                    bodiesById.TryGetValue(rel.ParentUniqueId, out var parentBody)
                    && bodiesById.TryGetValue(rel.ChildUniqueId, out var childBody)
                    && !string.IsNullOrEmpty(parentBody.PublicBodyId)
                    && parentBody.PublicBodyId == childBody.PublicBodyId;
            }
        }
    }
}
