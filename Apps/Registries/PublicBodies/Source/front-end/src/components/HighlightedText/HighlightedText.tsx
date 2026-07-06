import "./HighlightedText.css";

interface HighlightProps {
    text: string;
    search: string;
}

export function HighlightedText({ text, search }: HighlightProps) {
    if (!search.trim()) return <>{text}</>;

    // Escape special regex characters to prevent crashes
    const escapedSearch = search.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
    const regex = new RegExp(`(${escapedSearch})`, "gi");
    const parts = text.split(regex);

    return (
        <>
            {parts.map((part, index) =>
                regex.test(part) ? <mark key={index}>{part}</mark> : part,
            )}
        </>
    );
}
