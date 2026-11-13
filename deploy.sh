#!/usr/bin/env bash
# Update the production stack on the server: cd /opt/akiron-seo && ./deploy.sh
set -euo pipefail
cd "$(dirname "$0")"

git pull --ff-only
docker compose -f docker-compose.prod.yml up -d --build
docker image prune -f >/dev/null

for _ in $(seq 1 30); do
  if docker exec seo-backend curl -fsS http://localhost:5248/health >/dev/null 2>&1; then
    echo "seo is up"
    exit 0
  fi
  sleep 3
done
echo "seo-backend did not become ready; see: docker logs seo-backend --tail 100" >&2
exit 1
