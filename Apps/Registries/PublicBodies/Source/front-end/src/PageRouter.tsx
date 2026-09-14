import { Route, Switch } from "wouter";
import Home from "@/components/Home/Home";
import PublicBodiesList from "@/components/PublicBodiesList/PublicBodiesList";
import PublicBodyHistory from "@/components/PublicBodyHistory/PublicBodyHistory";
import DictionaryTable from "@/components/DictionaryTable/DictionaryTable";
import GlossaryList from "@/components/GlossaryList/GlossaryList";
import GlossaryDraftList from "@/components/GlossaryDraftList/GlossaryDraftList";
import GlossaryDraftEditor from "@/components/GlossaryDraftEditor/GlossaryDraftEditor";
import GlossaryTermEditor from "@/components/GlossaryTermEditor/GlossaryTermEditor";
import GlossaryTermHistory from "@/components/GlossaryTermHistory/GlossaryTermHistory";

export default function PageRouter() {
    return (
        <Switch>
            <Route path="/" component={Home} />

            <Route path="/public-bodies" component={PublicBodiesList} />

            <Route path="/public-bodies/:id/history">
                {(params) => <PublicBodyHistory id={params.id} />}
            </Route>

            <Route path="/dictionary" component={DictionaryTable} />

            <Route path="/glossary" component={GlossaryList} />

            <Route path="/glossary/versions/:glossaryVersion/terms/:term/versions">
                {(params) => (
                    <GlossaryTermHistory
                        glossaryVersion={params.glossaryVersion}
                        term={params.term}
                        selectedTermVersion={Number(
                            new URLSearchParams(window.location.search).get(
                                "selectedTermVersion",
                            ),
                        )}
                    />
                )}
            </Route>

            <Route path="/glossary/drafts" component={GlossaryDraftList} />

            <Route path="/glossary/drafts/:draftId/terms/new">
                {(params) => (
                    <GlossaryTermEditor draftId={params.draftId} isNew />
                )}
            </Route>

            <Route path="/glossary/drafts/:draftId/terms/:term">
                {(params) => (
                    <GlossaryTermEditor
                        draftId={params.draftId}
                        termSlug={params.term}
                    />
                )}
            </Route>

            <Route path="/glossary/drafts/:draftId">
                {(params) => <GlossaryDraftEditor draftId={params.draftId} />}
            </Route>

            {/* Default route in a switch */}
            <Route>404: No such page!</Route>
        </Switch>
    );
}
