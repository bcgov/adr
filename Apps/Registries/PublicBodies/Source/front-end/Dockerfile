# Dockerfile

# -----------
# Build stage
# -----------
FROM node:24-alpine3.23 AS build-stage

# Copy package.json and package-lock.json
WORKDIR /app
COPY package.json package-lock.json ./

# Print npm config
RUN npm config list

# Install dependencies
RUN npm ci

# Copy required files
COPY . .

# Build arguments required by the Vite build
ARG VITE_PUBLIC_BODIES_API_URL

# Run Vite build script, which places built files in /app/dist
RUN VITE_PUBLIC_BODIES_API_URL=${VITE_PUBLIC_BODIES_API_URL} npm run build

# -----------
# Serve stage
# -----------
FROM nginxinc/nginx-unprivileged:alpine3.22 AS serve-stage

# Copy nginx configuration
COPY ./nginx.conf /etc/nginx/nginx.conf

# Copy built Vite files to nginx public web root folder
COPY --from=build-stage /app/dist /usr/share/nginx/html

EXPOSE 8080
CMD ["nginx", "-g", "daemon off;"]
