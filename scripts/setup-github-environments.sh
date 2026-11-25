#!/usr/bin/env bash
# Creates the "staging" and "production" GitHub environments and fills their secrets and variables.
#
# 1. Install the GitHub CLI and sign in:   gh auth login
# 2. Replace every REPLACE_ME below with the real value (MonsterASP panel, your own JWT key, ...).
# 3. Run from the repository root:         bash scripts/setup-github-environments.sh
#
# Do NOT commit this file after filling in real values. Keep the filled copy outside the repository.
set -euo pipefail

# ---------------------------------------------------------------- STAGING
STAGING_BASE_URL="https://REPLACE_ME-staging.example.com"                                   # public URL of the staging API
STAGING_WEBDEPLOY_SERVER="https://REPLACE_ME.siteasp.net:8172"                              # MonsterASP > Websites > Web Deploy (with port)
STAGING_WEBDEPLOY_SITE="REPLACE_ME"                                                         # MonsterASP site name, e.g. site12345
STAGING_WEBDEPLOY_USERNAME="REPLACE_ME"
STAGING_WEBDEPLOY_PASSWORD="REPLACE_ME"
STAGING_DB_CONNECTION_STRING="Server=REPLACE_ME;Database=REPLACE_ME;User Id=REPLACE_ME;Password=REPLACE_ME;TrustServerCertificate=True;"
STAGING_JWT_KEY="REPLACE_ME_with_at_least_32_random_characters"                              # e.g. openssl rand -base64 48
STAGING_MEDIATR_LICENSE_KEY=""                                                              # optional

# ---------------------------------------------------------------- PRODUCTION (use different values from staging!)
PROD_BASE_URL="https://REPLACE_ME.example.com"
PROD_WEBDEPLOY_SERVER="https://REPLACE_ME.siteasp.net:8172"
PROD_WEBDEPLOY_SITE="REPLACE_ME"
PROD_WEBDEPLOY_USERNAME="REPLACE_ME"
PROD_WEBDEPLOY_PASSWORD="REPLACE_ME"
PROD_DB_CONNECTION_STRING="Server=REPLACE_ME;Database=REPLACE_ME;User Id=REPLACE_ME;Password=REPLACE_ME;TrustServerCertificate=True;"
PROD_JWT_KEY="REPLACE_ME_with_a_different_random_key_of_32_plus_chars"
PROD_MEDIATR_LICENSE_KEY=""

# ----------------------------------------------------------------
configure() {
  local env="$1" base_url="$2" server="$3" site="$4" user="$5" pass="$6" db="$7" jwt="$8" mediatr="$9"

  if [[ "$base_url$server$site$user$pass$db$jwt" == *REPLACE_ME* ]]; then
    echo "Skipping '$env': it still contains REPLACE_ME values."
    return 0
  fi

  echo "Configuring environment '$env' ..."
  gh api -X PUT "repos/{owner}/{repo}/environments/$env" >/dev/null

  gh variable set BASE_URL --env "$env" --body "$base_url"

  gh secret set WEBDEPLOY_SERVER   --env "$env" --body "$server"
  gh secret set WEBDEPLOY_SITE     --env "$env" --body "$site"
  gh secret set WEBDEPLOY_USERNAME --env "$env" --body "$user"
  gh secret set WEBDEPLOY_PASSWORD --env "$env" --body "$pass"
  gh secret set DB_CONNECTION_STRING --env "$env" --body "$db"
  gh secret set JWT_KEY            --env "$env" --body "$jwt"
  if [[ -n "$mediatr" ]]; then gh secret set MEDIATR_LICENSE_KEY --env "$env" --body "$mediatr"; fi
}

configure staging    "$STAGING_BASE_URL" "$STAGING_WEBDEPLOY_SERVER" "$STAGING_WEBDEPLOY_SITE" "$STAGING_WEBDEPLOY_USERNAME" "$STAGING_WEBDEPLOY_PASSWORD" "$STAGING_DB_CONNECTION_STRING" "$STAGING_JWT_KEY" "$STAGING_MEDIATR_LICENSE_KEY"
configure production "$PROD_BASE_URL"    "$PROD_WEBDEPLOY_SERVER"    "$PROD_WEBDEPLOY_SITE"    "$PROD_WEBDEPLOY_USERNAME"    "$PROD_WEBDEPLOY_PASSWORD"    "$PROD_DB_CONNECTION_STRING"    "$PROD_JWT_KEY"    "$PROD_MEDIATR_LICENSE_KEY"

echo
echo "Done. Two things still need the GitHub website (Settings > Environments > production):"
echo "  - add yourself under 'Required reviewers' so every production deploy needs your approval"
echo "  - under 'Deployment branches and tags' allow only tags matching v*.*.*"
