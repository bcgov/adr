import { Header, Footer } from "@bcgov/design-system-react-components";

import "./App.css";
import PageRouter from "./PageRouter";

function App() {
  return (
    <>
      <Header title="B.C. Public Bodies Register" />
      <main>
        <PageRouter />
      </main>
      <Footer />
    </>
  );
}

export default App;
