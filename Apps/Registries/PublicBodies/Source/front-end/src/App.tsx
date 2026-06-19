import { Header, Footer } from "@bcgov/design-system-react-components";
import { Link } from "wouter";

import "./App.css";
import PageRouter from "./PageRouter";

function App() {
    return (
        <>
            <Header title="B.C. Public Bodies Register" />
            <nav className="nav-links">
                <Link href="/public-bodies">Public Bodies</Link>
                <Link href="/public-bodies-chefs-form">CHEFS Form</Link>
                <Link href="/dictionary">Dictionary</Link>
                <Link href="/glossary">Glossary</Link>
            </nav>
            <main>
                <PageRouter />
            </main>
            <Footer />
        </>
    );
}

export default App;
