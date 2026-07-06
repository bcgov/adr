import { useMemo, useState } from "react";
import {
    Button,
    ProgressBar,
    TextField,
} from "@bcgov/design-system-react-components";
import {
    flexRender,
    getCoreRowModel,
    getFilteredRowModel,
    useReactTable,
} from "@tanstack/react-table";

import { DEVHUB_URL } from "@/constants";
import Main from "../Main/Main";
import { HighlightedText } from "../HighlightedText/HighlightedText";
import useDictionary from "@/hooks/useDictionary";
import useGlossary from "@/hooks/useGlossary";
import type { DictionaryField } from "@/models/dictionary";

import "./DictionaryTable.css";

type DictionaryRow = DictionaryField & { sourceEntry: string };

function termIdFromRef(ref: string | null | undefined): string | undefined {
    if (!ref) return undefined;
    const segments = ref.split("/").filter(Boolean);
    return segments[segments.length - 1];
}

export default function DictionaryTable() {
    const { data, error, isFetching, isPending } = useDictionary();
    const { data: glossaryData } = useGlossary();

    // Map of term-id → display term name, for resolving semanticTermRef links.
    const glossaryByTermId = useMemo(() => {
        const map = new Map<string, string>();
        for (const entry of glossaryData ?? []) {
            if (entry.id && entry.term) {
                map.set(entry.id, entry.term);
            }
        }
        return map;
    }, [glossaryData]);

    const rows: DictionaryRow[] = useMemo(() => {
        if (!data) return [];
        return data.flatMap((dictionary) =>
            (dictionary.entries ?? []).flatMap((entry) =>
                (entry.fields ?? []).map((field) => ({
                    ...field,
                    sourceEntry: entry.name ?? entry.id ?? "",
                })),
            ),
        );
    }, [data]);

    const columns = [
        { header: "Source", accessorKey: "sourceEntry" },
        { header: "Field Name", accessorKey: "fieldName" },
        {
            header: "Glossary",
            id: "glossary",
            accessorFn: (row: DictionaryRow) => {
                const termId = termIdFromRef(row.semanticTermRef);
                if (!termId) return "";
                return glossaryByTermId.get(termId) ?? termId;
            },
            cell: ({
                row,
                getValue,
            }: {
                row: { original: DictionaryRow };
                getValue: () => unknown;
            }) => {
                const termId = termIdFromRef(row.original.semanticTermRef);
                if (!termId) return null;
                const display = String(getValue() ?? termId);
                return (
                    <a
                        href={`${DEVHUB_URL}/docs/default/component/authoritative-data-registers/glossary-list/#term-${encodeURIComponent(termId)}`}
                        target="_blank"
                        rel="noopener noreferrer"
                    >
                        <HighlightedText text={display} search={globalFilter} />
                    </a>
                );
            },
        },
        { header: "Description", accessorKey: "fieldDescription" },
        { header: "Schema/Table", accessorKey: "schemaNameTableName" },
        { header: "Data Source", accessorKey: "dataSource" },
        { header: "Data Type", accessorKey: "dataType" },
        { header: "Key Relationships", accessorKey: "keyRelationships" },
        { header: "System of Record", accessorKey: "systemOfRecord" },
        { header: "Required", accessorKey: "designatedAsRequired" },
    ];

    const [layout, setLayout] = useState<"fixed" | "fluid">("fixed");
    const [globalFilter, setGlobalFilter] = useState("");

    const table = useReactTable({
        data: rows,
        columns,
        state: { globalFilter },
        onGlobalFilterChange: setGlobalFilter,
        getCoreRowModel: getCoreRowModel(),
        getFilteredRowModel: getFilteredRowModel(),
    });

    if (isPending)
        return (
            <Main>
                <ProgressBar
                    isIndeterminate
                    size="medium"
                    valueLabel="Loading..."
                />
            </Main>
        );

    if (error)
        return (
            <Main>
                <p>An error has occurred: {error.message}</p>
            </Main>
        );

    return (
        <Main layout={layout}>
            {isFetching && (
                <ProgressBar
                    isIndeterminate
                    size="medium"
                    valueLabel="Fetching data..."
                />
            )}
            <div className="dictionary-search-bar">
                <TextField
                    label="Search: "
                    type="search"
                    value={globalFilter}
                    onChange={(value) => setGlobalFilter(value)}
                />
                <Button
                    variant="secondary"
                    onPress={() =>
                        setLayout(layout === "fixed" ? "fluid" : "fixed")
                    }
                >
                    {layout === "fixed"
                        ? "Expand table view"
                        : "Collapse table view"}
                </Button>
            </div>
            <div className="dictionary-table-wrapper">
                <table className="dictionary-table">
                    {/*
                        Columns
                        -------
                        Source
                        Field Name
                        Glossary
                        Description
                        Schema/Table
                        Data Source
                        Data Type
                        Key Relationships
                        System of Record
                        Required
                    */}
                    <colgroup>
                        <col />
                        <col className="col-field-name" />
                        <col />
                        <col className="col-description" />
                        <col />
                        <col />
                        <col />
                        <col />
                        <col />
                        <col />
                    </colgroup>
                    <thead>
                        {table.getHeaderGroups().map((headerGroup) => (
                            <tr key={headerGroup.id}>
                                {headerGroup.headers.map((header) => (
                                    <th key={header.id}>
                                        {flexRender(
                                            header.column.columnDef.header,
                                            header.getContext(),
                                        )}
                                    </th>
                                ))}
                            </tr>
                        ))}
                    </thead>
                    <tbody>
                        {table.getRowModel().rows.map((row) => (
                            <tr key={row.id}>
                                {row.getVisibleCells().map((cell) => (
                                    <td key={cell.id}>
                                        {cell.column.id === "glossary" ? (
                                            flexRender(
                                                cell.column.columnDef.cell,
                                                cell.getContext(),
                                            )
                                        ) : (
                                            <HighlightedText
                                                text={String(
                                                    cell.getValue() ?? "",
                                                )}
                                                search={globalFilter}
                                            />
                                        )}
                                    </td>
                                ))}
                            </tr>
                        ))}
                    </tbody>
                </table>
            </div>
        </Main>
    );
}
