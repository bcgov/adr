import { Header, Footer } from "@bcgov/design-system-react-components";

import PublicBodiesList from "./components/PublicBodiesList/PublicBodiesList";

import "./App.css";

function App() {
  return (
    <>
      <Header title="B.C. Public Bodies Register" />
      <main>
        <PublicBodiesList />
      </main>
      <Footer />
    </>
  );
}

export default App;
