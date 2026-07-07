import Main from "../Main/Main";

export default function Home() {
    return (
        <Main>
            <p>
                This is the landing page for the Public Bodies Register demo app
                from the{" "}
                <a
                    href="https://www.github.com/bcgov/adr"
                    target="_blank"
                    rel="noopener noreferrer"
                >
                    Authoritative Data Registers
                </a>{" "}
                team in Connected Services BC.
            </p>
            <p> Choose a view from the navigation bar above.</p>
        </Main>
    );
}
