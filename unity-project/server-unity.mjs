import http from "http";
import fs from "fs";
import path from "path";

const dir = path.join(
  path.dirname(new URL(import.meta.url).pathname).replace(/^\/([A-Z]:)/, "$1"),
  "Build", "WebGL"
);

const MIME = {
  ".html": "text/html",
  ".js": "application/javascript",
  ".wasm": "application/wasm",
  ".data": "application/octet-stream",
  ".unityweb": "application/octet-stream",
  ".png": "image/png",
  ".ico": "image/x-icon",
  ".json": "application/json",
  ".css": "text/css",
};

const server = http.createServer((req, res) => {
  let fp = path.join(dir, req.url === "/" ? "/index.html" : req.url);
  if (!fs.existsSync(fp)) {
    res.writeHead(404);
    res.end("Not found");
    return;
  }
  const ext = path.extname(fp);
  res.setHeader("Cross-Origin-Opener-Policy", "same-origin");
  res.setHeader("Cross-Origin-Embedder-Policy", "require-corp");
  res.setHeader("Content-Type", MIME[ext] || "application/octet-stream");
  fs.createReadStream(fp).pipe(res);
});

server.listen(8061, () => console.log("Unity WebGL server: http://localhost:8061"));
