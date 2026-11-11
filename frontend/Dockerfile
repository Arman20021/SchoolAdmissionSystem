
# ==========================================
# Stage 1: Build React Application
# ==========================================
FROM node:22-alpine AS build

WORKDIR /app

# Copy dependency manifests
COPY package*.json ./

# Install dependencies
RUN npm ci

# Copy frontend source
COPY . .

# Backend URL available during Vite build
ARG VITE_API_BASE_URL=http://localhost:8080

ENV VITE_API_BASE_URL=${VITE_API_BASE_URL}

# Build React application
RUN npm run build


# ==========================================
# Stage 2: Nginx Production Server
# ==========================================
FROM nginx:alpine AS final

COPY nginx.conf /etc/nginx/conf.d/default.conf

COPY --from=build /app/dist /usr/share/nginx/html

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]
