import { useMemo, useState } from "react";
import useOpenApi from "../../hooks/useOpenApi";
import "./DictionaryTable.css";
import {
  flexRender,
  getCoreRowModel,
  getFilteredRowModel,
  useReactTable,
} from "@tanstack/react-table";
import { extractDictionary, type DictionaryField } from "./util";

export default function DictionaryTable() {
  const { data, error, isFetching, isPending } = useOpenApi();

  const bcDictionary: DictionaryField[] = useMemo(() => {
    return extractDictionary(data) || [];
  }, [data]);

  const columns = [
    {
      header: "Field Name",
      accessorKey: "fieldName",
    },
    {
      header: "Description",
      accessorKey: "fieldDescription",
    },
    {
      header: "Schema/Table",
      accessorKey: "schemaNameTableName",
    },
    {
      header: "Data Source",
      accessorKey: "dataSource",
    },
    {
      header: "Data Type",
      accessorKey: "dataType",
    },
    {
      header: "Key Relationships",
      accessorKey: "keyRelationships",
    },
    {
      header: "System of Record",
      accessorKey: "systemOfRecord",
    },
    {
      header: "Required",
      accessorKey: "designatedAsRequired",
    },
  ];

  const [globalFilter, setGlobalFilter] = useState("");

  const table = useReactTable({
    data: bcDictionary,
    columns,
    state: {
      globalFilter,
    },
    onGlobalFilterChange: setGlobalFilter,
    getCoreRowModel: getCoreRowModel(),
    getFilteredRowModel: getFilteredRowModel(),
  });

  if (isFetching) return "Fetching data...";

  if (isPending) return "Loading...";

  if (error) return "An error has occurred: " + error.message;

  return (
    <div>
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
      <table className="dictionary-table">
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
                  {flexRender(cell.column.columnDef.cell, cell.getContext())}
                </td>
              ))}
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
