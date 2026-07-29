FROM node:22-alpine AS build
WORKDIR /source

COPY src/ProjetoPizza.Web/package.json src/ProjetoPizza.Web/package-lock.json ./
RUN npm ci

COPY src/ProjetoPizza.Web/ ./
ARG VITE_API_URL=/backend
ENV VITE_API_URL=$VITE_API_URL
RUN npm run build

FROM nginx:1.28-alpine AS runtime
COPY docker/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /source/dist /usr/share/nginx/html

EXPOSE 80
