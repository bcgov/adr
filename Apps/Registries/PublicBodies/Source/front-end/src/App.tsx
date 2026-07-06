import {
    Header,
    Subheader,
    Footer,
} from "@bcgov/design-system-react-components";
import { Link } from "wouter";

import "./App.css";
import PageRouter from "./PageRouter";

function App() {
    return (
        <>
            <Header title="B.C. Public Bodies Register" />
            <Subheader>
                <Link href="/public-bodies" className="nav-link">
                    Public Bodies
                </Link>
                <Link href="/dictionary" className="nav-link">
                    Dictionary
                </Link>
                <Link href="/glossary" className="nav-link">
                    Glossary
                </Link>
            </Subheader>
            <PageRouter />
            <Footer />
        </>
    );
}

export default App;
