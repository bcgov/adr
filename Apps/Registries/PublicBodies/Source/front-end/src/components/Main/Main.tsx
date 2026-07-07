import type { PropsWithChildren } from "react";

import "./Main.css";

interface MainProps {
    /**
     * `fixed` layout clamps to header and footer width,
     * `fluid` layout takes full width.
     * */
    layout?: "fixed" | "fluid";
}

export default function Main({
    children,
    layout = "fixed",
}: PropsWithChildren<MainProps>) {
    return <main className={layout}>{children}</main>;
}
