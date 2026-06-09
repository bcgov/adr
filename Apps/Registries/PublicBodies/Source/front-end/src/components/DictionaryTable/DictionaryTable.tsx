import { useMemo, useState } from "react";
import { Link } from "wouter";
import {
    flexRender,
    getCoreRowModel,
    getFilteredRowModel,
    useReactTable,
} from "@tanstack/react-table";

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
            accessorKey: "semanticTermRef",
            cell: ({ getValue }: { getValue: () => unknown }) => {
                const termId = termIdFromRef(getValue() as string | undefined);
                if (!termId) return null;
                const display = glossaryByTermId.get(termId) ?? termId;
                return (
                    <Link href={`/glossary#${encodeURIComponent(termId)}`}>
                        {display}
                    </Link>
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

    const [globalFilter, setGlobalFilter] = useState("");

    const table = useReactTable({
        data: rows,
        columns,
        state: { globalFilter },
        onGlobalFilterChange: setGlobalFilter,
        getCoreRowModel: getCoreRowModel(),
        getFilteredRowModel: getFilteredRowModel(),
    });

    if (isPending) return "Loading...";
    if (error) return "An error has occurred: " + error.message;

    return (
        <div>
            {isFetching && <span>Fetching data...</span>}
            <span>
                Search:
                <input
                    className="dictionary-search"
                    type="text"
                    placeholder=""
                    value={globalFilter}
                    onChange={(e) => setGlobalFilter(e.target.value)}
                />
            </span>
            <div className="dictionary-table-wrapper">
                <table className="dictionary-table">
                    <colgroup>
                        <col /> {/* Source */}
                        <col className="col-field-name" />
                        <col /> {/* Glossary */}
                        <col className="col-description" />
                        <col /> {/* Schema/Table */}
                        <col /> {/* Data Source */}
                        <col /> {/* Data Type */}
                        <col /> {/* Key Relationships */}
                        <col /> {/* System of Record */}
                        <col /> {/* Required */}
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
                                        {flexRender(
                                            cell.column.columnDef.cell,
                                            cell.getContext(),
                                        )}
                                    </td>
                                ))}
                            </tr>
                        ))}
                    </tbody>
                </table>
            </div>
        </div>
    );
}
