namespace Adr.PublicBodies.Services
{
    using System;
    using System.Collections.Generic;
    using Adr.PublicBodies.Models;
    using Microsoft.Extensions.Logging;

    public class HardcodedMinistryService : IMinistryService
    {
        private readonly ILogger<HardcodedMinistryService> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="HardcodedMinistryService"/> class.
        /// </summary>
        /// <param name="logger">Injected Logger Provider.</param>
        public HardcodedMinistryService(ILogger<HardcodedMinistryService> logger)
        {
            _logger = logger;
        }

        /// <inheritdoc/>
        public IEnumerable<MinistryModel> GetAll()
        {
            var ministries = new List<MinistryModel>();

            ministries.Add(
                new MinistryModel()
                {
                    Id = "019b2cf6-2e87-7e61-b3b5-bf786d1b6949",
                    Name = "Agriculture and Food",
                    Acronym = "AF",
                }
            );
            ministries.Add(
                new MinistryModel()
                {
                    Id = "019b2cf6-2e87-7ac3-89cb-65c282b6e360",
                    Name = "Attorney General",
                    Acronym = "AG",
                }
            );
            ministries.Add(
                new MinistryModel()
                {
                    Id = "019b2cf6-2e87-7824-8c1a-96ef7e2e592f",
                    Name = "Children and Family Development",
                    Acronym = "CFD",
                }
            );
            ministries.Add(
                new MinistryModel()
                {
                    Id = "019b2cf6-2e87-797a-8c0d-1a3860afd0b0",
                    Name = "Citizens' Services",
                    Acronym = "CITZ",
                }
            );
            ministries.Add(
                new MinistryModel()
                {
                    Id = "019b2cf6-2e87-73ed-87c3-91ef62e3647c",
                    Name = "Education and Child Care",
                    Acronym = "ECC",
                }
            );
            ministries.Add(
                new MinistryModel()
                {
                    Id = "019b2cf6-2e87-7c3c-bd52-185cd0a69c69",
                    Name = "Emergency Management and Climate Readiness",
                    Acronym = "EMCR",
                }
            );
            ministries.Add(
                new MinistryModel()
                {
                    Id = "019b2cf6-2e87-7e85-9a05-1b466815fafb",
                    Name = "Energy, Mines and Low Carbon Innovation",
                    Acronym = "",
                    RetirementDate = DateOnly.Parse("2024/11/17"),
                }
            );
            ministries.Add(
                new MinistryModel()
                {
                    Id = "019b2cf6-2e87-7e85-9a05-1b466815fafb",
                    Name = "Energy and Climate Solutions",
                    Acronym = "ECS",
                    EffectiveDate = DateOnly.Parse("2024/11/18"),
                }
            );
            ministries.Add(
                new MinistryModel()
                {
                    Id = "019b2cf6-2e87-7130-b323-1e409183772c",
                    Name = "Environment and Climate Change Strategy",
                    Acronym = "",
                    RetirementDate = DateOnly.Parse("2024/11/17"),
                }
            );
            ministries.Add(
                new MinistryModel()
                {
                    Id = "019b2cf6-2e87-7130-b323-1e409183772c",
                    Name = "Environment and Parks",
                    Acronym = "EP",
                    EffectiveDate = DateOnly.Parse("2024/11/18"),
                }
            );
            ministries.Add(
                new MinistryModel()
                {
                    Id = "019b2cf6-2e87-7475-af6e-0d01e0583eeb",
                    Name = "Finance",
                    Acronym = "FIN",
                }
            );
            ministries.Add(
                new MinistryModel()
                {
                    Id = "019b2cf6-2e87-748e-a2e7-c1d99ca76c3f",
                    Name = "Forests",
                    Acronym = "FOR",
                }
            );
            ministries.Add(
                new MinistryModel()
                {
                    Id = "019b2cf6-2e87-7131-bee9-df432312fb7d",
                    Name = "Health",
                    Acronym = "HLTH",
                }
            );
            ministries.Add(
                new MinistryModel()
                {
                    Id = "019b2cf6-2e87-7e3c-86e0-8d046c71d789",
                    Name = "Housing",
                    Acronym = "",
                    RetirementDate = DateOnly.Parse("2024/11/17"),
                }
            );
            ministries.Add(
                new MinistryModel()
                {
                    Id = "019b2cf6-2e87-7e3c-86e0-8d046c71d789",
                    Name = "Housing and Municipal Affairs",
                    Acronym = "HMA",
                    EffectiveDate = DateOnly.Parse("2024/11/18"),
                }
            );
            ministries.Add(
                new MinistryModel()
                {
                    Id = "019b2cf6-2e87-705b-bfd1-d5a06ec50be5",
                    Name = "Indigenous Relations and Reconciliation",
                    Acronym = "IRR",
                }
            );
            ministries.Add(
                new MinistryModel()
                {
                    Id = "019b2cf6-2e87-7d1f-8181-cb2be70a02fe",
                    Name = "Infrastructure",
                    Acronym = "INF",
                    EffectiveDate = DateOnly.Parse("2024/11/18"),
                }
            );
            ministries.Add(
                new MinistryModel()
                {
                    Id = "019b2cf6-2e87-77ee-8eb2-99f9407ef13a",
                    Name = "Jobs and Economic Growth",
                    Acronym = "JEG",
                    EffectiveDate = DateOnly.Parse("2024/07/17"),
                }
            );
            ministries.Add(
                new MinistryModel()
                {
                    Id = "019b2cf6-2e87-77ee-8eb2-99f9407ef13a",
                    Name = "Jobs, Economic Development and Innovation",
                    Acronym = "JEDI",
                    RetirementDate = DateOnly.Parse("2024/07/16"),
                }
            );
            ministries.Add(
                new MinistryModel()
                {
                    Id = "019b2cf6-2e87-7c68-8336-738d852705f4",
                    Name = "Labour",
                    Acronym = "LBR",
                }
            );
            ministries.Add(
                new MinistryModel()
                {
                    Id = "019b2cf6-2e87-7932-9130-bf7002573744",
                    Name = "Mental Health and Addictions",
                    Acronym = "",
                    RetirementDate = DateOnly.Parse("2024/11/17"),
                }
            );
            ministries.Add(
                new MinistryModel()
                {
                    Id = "019b2cf6-2e87-7b6f-b662-f613158255a2",
                    Name = "Mining and Critical Minerals",
                    Acronym = "MCM",
                    EffectiveDate = DateOnly.Parse("2024/11/18"),
                }
            );
            ministries.Add(
                new MinistryModel()
                {
                    Id = "019b2cf6-2e87-78f4-abeb-abd2876abc49",
                    Name = "Municipal Affairs",
                    Acronym = "",
                    RetirementDate = DateOnly.Parse("2024/11/17"),
                }
            );
            ministries.Add(
                new MinistryModel()
                {
                    Id = "019b2cf6-2e87-7db4-9ca2-3ebf4fa34678",
                    Name = "Post-Secondary Education and Future Skills",
                    Acronym = "PSFS",
                }
            );
            ministries.Add(
                new MinistryModel()
                {
                    Id = "019b2cf6-2e87-7d8d-bc60-a3b0794f0e93",
                    Name = "Public Safety and Solicitor General",
                    Acronym = "PSSG",
                }
            );
            ministries.Add(
                new MinistryModel()
                {
                    Id = "019b2cf6-2e87-7571-a9b3-ff80b0c007c1",
                    Name = "Social Development and Poverty Reduction",
                    Acronym = "SDPR",
                }
            );
            ministries.Add(
                new MinistryModel()
                {
                    Id = "019b2cf6-2e87-7726-a84e-b8cb7bc6f0bc",
                    Name = "Tourism, Arts, Culture and Sport",
                    Acronym = "TACS",
                }
            );
            ministries.Add(
                new MinistryModel()
                {
                    Id = "019b2cf6-2e87-79b9-9597-b81453687726",
                    Name = "Transportation and Infrastructure",
                    Acronym = "MOTI",
                    RetirementDate = DateOnly.Parse("2024/11/17"),
                }
            );
            ministries.Add(
                new MinistryModel()
                {
                    Id = "019b2cf6-2e87-79b9-9597-b81453687726",
                    Name = "Transportation and Transit",
                    Acronym = "TT",
                    EffectiveDate = DateOnly.Parse("2024/11/18"),
                }
            );
            ministries.Add(
                new MinistryModel()
                {
                    Id = "019b2cf6-2e87-7b17-acca-27d95d384cf9",
                    Name = "Water, Land and Resource Stewardship",
                    Acronym = "WLRS",
                }
            );

            return ministries;
        }
    }
}
