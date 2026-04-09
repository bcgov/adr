# Dockerfile

# -----------
# Build stage
# -----------
FROM docker.io/library/node:24-alpine3.23 AS build-stage

# Copy package.json and package-lock.json
WORKDIR /app
COPY package.json package-lock.json ./

# Print npm config
RUN npm config list

# Install dependencies
RUN npm ci

# Copy required files
COPY . .

# Run Vite build script, which places built files in /app/dist.
# Note: Variables are intentionally NOT passed at build time.
# Env is injected at start by entrypoint.sh → /usr/share/nginx/html/config.js → window.<vars> (see src/constants.ts).
RUN npm run build

# -----------
# Serve stage
# -----------
FROM docker.io/nginxinc/nginx-unprivileged:alpine3.22 AS serve-stage

# Copy nginx configuration
COPY ./nginx.conf /etc/nginx/nginx.conf

# Owned by nginx (UID 101) so the runtime entrypoint can rewrite config.js.
# Numeric IDs bypass buildah's host-side username lookup.
COPY --chown=101:101 --from=build-stage /app/dist /usr/share/nginx/html

# Install the runtime config writer into nginx's startup hook directory. The
# nginx-unprivileged base image runs every executable script in
# /docker-entrypoint.d/ before starting nginx.
# Note:
#   script use 40-<name> to guarantee that it runs after the base images startup scripts and
#   before new scripts are added.
COPY --chmod=755 entrypoint.sh /docker-entrypoint.d/40-write-runtime-config.sh

EXPOSE 8080
CMD ["nginx", "-g", "daemon off;"]
