import { spawn } from "node:child_process";
import { once } from "node:events";
import { mkdtemp, readFile, rm } from "node:fs/promises";
import { createServer } from "node:net";
import { dirname, join, resolve } from "node:path";
import { tmpdir } from "node:os";
import { fileURLToPath } from "node:url";

const testDirectory = dirname(fileURLToPath(import.meta.url));
const apiProject = resolve(testDirectory, "../../Source/Api/Semantics.csproj");
const exampleDocuments = [
  {
    name: "standalone term schema",
    path: resolve(testDirectory, "../../Examples/semantic-reference.openapi.yaml"),
  },
  {
    name: "versioned glossary component",
    path: resolve(testDirectory, "../../Examples/glossary-reference.openapi.yaml"),
  },
];
const ruleset = resolve(testDirectory, "semantic-reference-ruleset.yaml");
const spectralCli = resolve(
  testDirectory,
  "node_modules/@stoplight/spectral-cli/dist/index.js",
);

const delay = (milliseconds) => new Promise((resolveDelay) => setTimeout(resolveDelay, milliseconds));

async function findAvailablePort() {
  const server = createServer();
  await new Promise((resolveListen, rejectListen) => {
    server.once("error", rejectListen);
    server.listen(0, "127.0.0.1", resolveListen);
  });

  const address = server.address();
  const port = typeof address === "object" && address !== null ? address.port : null;
  await new Promise((resolveClose, rejectClose) => {
    server.close((error) => (error ? rejectClose(error) : resolveClose()));
  });

  if (port === null) {
    throw new Error("Could not allocate a local port for the Semantics API.");
  }

  return port;
}

async function waitForApi(apiUrl, apiProcess, readApiOutput) {
  for (let attempt = 0; attempt < 150; attempt += 1) {
    if (apiProcess.exitCode !== null) {
      throw new Error(`The Semantics API exited during startup.\n${readApiOutput()}`);
    }

    try {
      const response = await fetch(`${apiUrl}/health`);
      if (response.ok) {
        return;
      }
    } catch {
      // The API has not started listening yet.
    }

    await delay(200);
  }

  throw new Error(`Timed out waiting for the Semantics API.\n${readApiOutput()}`);
}

async function stopProcess(childProcess) {
  if (childProcess.exitCode !== null) {
    return;
  }

  childProcess.kill("SIGTERM");
  await Promise.race([once(childProcess, "exit"), delay(5_000)]);
  if (childProcess.exitCode === null) {
    childProcess.kill("SIGKILL");
  }
}

async function runSpectral(document, label, sourcePath) {
  const spectralProcess = spawn(
    process.execPath,
    [
      spectralCli,
      "lint",
      "-",
      "--stdin-filepath",
      sourcePath,
      "--ruleset",
      ruleset,
      "--fail-severity",
      "error",
      "--display-only-failures",
    ],
    { stdio: ["pipe", "pipe", "pipe"] },
  );

  let output = "";
  spectralProcess.stdout.on("data", (data) => {
    output += data;
  });
  spectralProcess.stderr.on("data", (data) => {
    output += data;
  });
  spectralProcess.stdin.end(document);

  const [exitCode] = await once(spectralProcess, "exit");
  if (exitCode !== 0) {
    throw new Error(`Spectral compatibility test failed for ${label}.\n${output}`);
  }

  process.stdout.write(`${label}: `);
  process.stdout.write(output);
}

const port = await findAvailablePort();
const apiUrl = `http://127.0.0.1:${port}`;
const temporaryDirectory = await mkdtemp(join(tmpdir(), "adr-semantics-spectral-"));
const databasePath = join(temporaryDirectory, "semantics.db");
const apiProcess = spawn(
  "dotnet",
  ["run", "--project", apiProject, "--no-build", "--no-launch-profile"],
  {
    env: {
      ...process.env,
      ASPNETCORE_URLS: apiUrl,
      ConnectionStrings__Semantics: `Data Source=${databasePath}`,
    },
    stdio: ["ignore", "pipe", "pipe"],
  },
);

let apiOutput = "";
apiProcess.stdout.on("data", (data) => {
  apiOutput += data;
});
apiProcess.stderr.on("data", (data) => {
  apiOutput += data;
});

try {
  await waitForApi(apiUrl, apiProcess, () => apiOutput);
  for (const exampleDocument of exampleDocuments) {
    const example = await readFile(exampleDocument.path, "utf8");
    const testDocument = example.replaceAll("http://localhost:5001", apiUrl);
    if (testDocument === example) {
      throw new Error(
        `${exampleDocument.path} does not contain the expected local Semantics API URL.`,
      );
    }

    const openApi31Document = testDocument.replace("openapi: 3.0.3", "openapi: 3.1.0");
    if (openApi31Document === testDocument) {
      throw new Error(`${exampleDocument.path} does not declare the expected OpenAPI version.`);
    }

    await runSpectral(
      testDocument,
      `${exampleDocument.name} / OpenAPI 3.0.3`,
      exampleDocument.path,
    );
    await runSpectral(
      openApi31Document,
      `${exampleDocument.name} / OpenAPI 3.1.0`,
      exampleDocument.path,
    );
  }
} catch (error) {
  process.stderr.write(`${error instanceof Error ? error.stack : error}\n`);
  process.exitCode = 1;
} finally {
  await stopProcess(apiProcess);
  await rm(temporaryDirectory, { recursive: true, force: true });
}
