plugins {
    alias(libs.plugins.android.application)
    alias(libs.plugins.kotlin.compose)
    alias(libs.plugins.compose.multiplatform)
}

android {
    namespace = "com.floristhub.android"
    compileSdk = 37

    defaultConfig {
        applicationId = "com.floristhub.android"
        minSdk = 26
        targetSdk = 36
        versionCode = (System.getenv("ANDROID_VERSION_CODE")
            ?: findProperty("floristHub.versionCode")?.toString()
            ?: "1").toInt()
        versionName = System.getenv("ANDROID_VERSION_NAME")
            ?: findProperty("floristHub.versionName")?.toString()
            ?: "0.1.0"
        val apiBaseUrl = project.findProperty("floristHub.apiBaseUrl")?.toString()
            ?: "http://10.0.2.2:5183"
        buildConfigField("String", "API_BASE_URL", "\"$apiBaseUrl\"")
        val webBaseUrl = project.findProperty("floristHub.webBaseUrl")?.toString()
            ?: "http://10.0.2.2:5173"
        buildConfigField("String", "WEB_BASE_URL", "\"$webBaseUrl\"")
        val tenantSlug = project.findProperty("floristHub.tenantSlug")?.toString()
            ?: "sao-flores"
        buildConfigField("String", "TENANT_SLUG", "\"$tenantSlug\"")
        manifestPlaceholders["usesCleartextTraffic"] = "false"
    }

    signingConfigs {
        create("release") {
            val path = System.getenv("ANDROID_KEYSTORE_PATH")
            if (!path.isNullOrBlank()) {
                storeFile = file(path)
                storePassword = System.getenv("ANDROID_KEYSTORE_PASSWORD")
                keyAlias = System.getenv("ANDROID_KEY_ALIAS")
                keyPassword = System.getenv("ANDROID_KEY_PASSWORD")
                    ?: System.getenv("ANDROID_KEYSTORE_PASSWORD")
            }
        }
    }

    buildTypes {
        debug {
            manifestPlaceholders["usesCleartextTraffic"] = "true"
        }
        release {
            manifestPlaceholders["usesCleartextTraffic"] = "false"
            val releaseSigning = signingConfigs.getByName("release")
            if (releaseSigning.storeFile != null) {
                signingConfig = releaseSigning
            }
        }
    }

    compileOptions {
        sourceCompatibility = JavaVersion.VERSION_17
        targetCompatibility = JavaVersion.VERSION_17
    }

    buildFeatures {
        compose = true
        buildConfig = true
    }
}

dependencies {
    implementation(project(":shared"))
    implementation(libs.kotlinx.coroutines.core)
    implementation(libs.firebase.messaging)
}
