import "./HighlightedText.css";

interface HighlightedTextProps {
    text: string;
    search: string;
}

export function HighlightedText({ text, search }: HighlightedTextProps) {
    if (!search.trim()) return <>{text}</>;

    // Escape special regex characters to prevent crashes
    const escapedSearch = search.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
    const regex = new RegExp(`(${escapedSearch})`, "gi");
    const parts = text.split(regex);

    return (
        <>
            {parts.map((part, index) =>
                // Matches are at odd indices after the
                // `.split()` when using a capturing group.
                index % 2 === 1 ? <mark key={index}>{part}</mark> : part,
            )}
        </>
    );
}
