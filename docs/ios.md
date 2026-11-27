# Florist Hub — app iOS (Kotlin Multiplatform)

Este documento descreve como abrir e correr a app iOS no Xcode num Mac, como testar num iPhone físico com conta Apple gratuita e o que é necessário para publicar na App Store.

## Requisitos

- macOS com **Xcode 15** ou mais recente (inclui simulador iOS e toolchain Swift)
- **JDK 17** (para o Gradle/Kotlin)
- Repositório clonado; pasta do projeto móvel: `mobile/`

A UI partilhada vive no módulo Gradle `:shared` (Compose Multiplatform). O projeto nativo **`mobile/iosApp`** é uma app SwiftUI que hospeda o `ComposeUIViewController` exposto em `MainViewController.kt`.

## Configurar a URL da API

No Android, a base URL vem de `floristHub.apiBaseUrl` em `mobile/gradle.properties` (`BuildConfig.API_BASE_URL`).

No iOS, o equivalente é a chave **`FLORIST_HUB_API_BASE_URL`** no `Info.plist` (ou em `iosApp/Configuration/Config.xcconfig`):

| Ambiente | Valor típico |
|----------|----------------|
| API na máquina local, simulador | `http://127.0.0.1:5183` |
| API na máquina local, dispositivo físico (mesma Wi‑Fi) | `http://<IP-do-Mac>:5183` |
| API remota / staging | `https://…` |

Altere `FLORIST_HUB_API_BASE_URL` antes de correr no Xcode. O simulador usa `127.0.0.1` para o localhost do Mac.

## Abrir e correr no Xcode

1. No terminal, na pasta `mobile/`:
   ```bash
   ./gradlew :shared:embedAndSignAppleFrameworkForXcode
   ```
   (O Xcode também pode invocar o script de build embutido no target para gerar o framework `shared`.)

2. Abra `mobile/iosApp/iosApp.xcodeproj` no Xcode.

3. Selecione o destino **iPhone Simulator** (por exemplo iPhone 16).

4. Confirme o **Bundle Identifier** em *Signing & Capabilities* (valor por defeito em `Config.xcconfig`: `com.floristhub.ios`).

5. **Run** (▶). A primeira compilação pode demorar enquanto o Gradle produz o framework Kotlin.

### Permissões

O `Info.plist` inclui texto de utilização para acesso à **biblioteca de fotografias** (upload de imagens do portefólio / logótipo). Não ative permissões extra sem necessidade.

## Testar num iPhone real sem programa de developer pago

Com uma **Apple ID gratuita** (conta iCloud):

1. Ligue o iPhone ao Mac e escolha o dispositivo como destino no Xcode.
2. Em *Signing & Capabilities*, escolha o seu **Team** (Personal Team).
3. No iPhone: *Definições → Privacidade e segurança → Modo de programador* (se aplicável) e confie no certificado de desenvolvedor quando pedido.
4. Instale e abra a app.

**Limitações da conta gratuita:** a app expira ao fim de **7 dias** e é preciso voltar a instalar a partir do Xcode. Há limite no número de apps “de desenvolvimento” activas por dispositivo.

Para testar contra a API local, use o IP do Mac em `FLORIST_HUB_API_BASE_URL`, não `127.0.0.1`.

## Publicar na App Store

Para distribuição além dos 7 dias ou TestFlight público:

- Inscrição no **Apple Developer Program** (**99 USD/ano**).
- Certificados de distribuição, perfil de aprovisionamento de App Store, ícones, capturas de ecrã, política de privacidade.
- **TestFlight** (beta) a partir do mesmo binário validado no App Store Connect.
- Revisão da App Store na submissão final.

O bundle id e a versão (`MARKETING_VERSION` / `CURRENT_PROJECT_VERSION` nos xcconfig) devem alinhar com o registo em App Store Connect.

## App Store sem Mac local

A ficha (textos, capturas, política de privacidade `https://<web>/legal/privacidade`) cria-se no browser, no App Store Connect, depois da inscrição no Apple Developer Program (99 USD/ano). O binário continua a precisar de um Mac com Xcode, ou de um workflow macOS que assine o IPA: o workflow manual que existe neste repositório compila o simulador e não envia nada à App Store. O guia completo está em [deploy.md](./deploy.md).

## Build iOS na cloud (sem Mac local)

Sem Mac local, use o workflow **「iOS (simulador)」** no GitHub Actions — passo a passo, artefactos e custo de minutos em **[ios-testes.md](./ios-testes.md)**.

TestFlight / dispositivo físico além de 7 dias continuam a exigir conta **Apple Developer (99 USD/ano)**; esqueleto fastlane desactivado em `mobile/iosApp/fastlane/Fastfile`.

## Verificação em Linux / CI sem Mac

Num ambiente sem Xcode:

- `./gradlew :shared:compileCommonMainKotlinMetadata`
- `./gradlew :shared:jvmTest` (testes de `commonTest`, incluindo regras de agendamento)
- `./gradlew :shared:compileKotlinIosArm64` (só se a toolchain iOS estiver disponível — em Linux costuma falhar por falta de SDK)

A ligação final (`xcodebuild`, simulador, dispositivo) **requer Mac**.
