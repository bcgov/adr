import type { PropsWithChildren } from "react";

import "./Main.css";

interface MainProps {
    layout?: "fixed" | "fluid";
}

export default function Main({
    children,
    layout = "fixed",
}: PropsWithChildren<MainProps>) {
    return <main className={layout}>{children}</main>;
}
