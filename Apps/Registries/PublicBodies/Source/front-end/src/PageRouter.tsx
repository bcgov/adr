import { Route, Switch } from "wouter";
import PublicBodiesList from "@/components/PublicBodiesList/PublicBodiesList";
import DictionaryTable from "@/components/DictionaryTable/DictionaryTable";
import GlossaryList from "@/components/GlossaryList/GlossaryList";

export default function PageRouter() {
    return (
        <Switch>
            <Route path="/public-bodies" component={PublicBodiesList} />

            <Route path="/dictionary" component={DictionaryTable} />

            <Route path="/glossary" component={GlossaryList} />

            {/* Default route in a switch */}
            <Route>404: No such page!</Route>
        </Switch>
    );
}
