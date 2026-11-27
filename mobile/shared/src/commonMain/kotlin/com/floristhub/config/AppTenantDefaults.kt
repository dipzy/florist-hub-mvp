package com.floristhub.config

/** Slug por omissão quando a plataforma não injecta outro (build da florista). */
const val DEFAULT_APP_TENANT_SLUG = "sao-flores"

fun normalizeTenantSlug(raw: String): String = raw.trim().lowercase()
