import { Route, Switch } from "wouter";
import Home from "./components/Home/Home";
import PublicBodiesList from "@/components/PublicBodiesList/PublicBodiesList";
import PublicBodyHistory from "@/components/PublicBodyHistory/PublicBodyHistory";
import DictionaryTable from "@/components/DictionaryTable/DictionaryTable";
import GlossaryList from "@/components/GlossaryList/GlossaryList";

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

            {/* Default route in a switch */}
            <Route>404: No such page!</Route>
        </Switch>
    );
}
