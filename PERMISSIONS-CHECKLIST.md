# Permissions Checklist — Setup completo per tutti i workflow del lab

> Checklist puntuale, **dal livello organization fino al personal**, di tutto cio' che serve per far girare al 100% i workflow del lab. Ogni voce indica: **dove** impostarla, **cosa** sblocca, **quale workflow** la richiede.

---

## Indice rapido — i workflow del lab

Riferimento alle sigle usate dopo nella checklist:

| Sigla | File | Opzione | Provider |
|-------|------|:---:|----------|
| **W1** | `scheduled-review.yml` | B | GitHub Models (action) |
| **W2** | `scheduled-review-curl.yml` | B | GitHub Models (curl) |
| **W3** | `review-and-upload-sftp.yml` | B | GitHub Models + SFTP |
| **W4** | `review-and-upload-http.yml` | B | GitHub Models + HTTP |
| **W5** | `scheduled-coding-agent-review.yml` | A | Copilot Coding Agent (PAT) |
| **W6** | `scheduled-coding-agent-review-app.yml` | A | Coding Agent (App + Service Account) |
| **W7** | `review-with-openai-api.yml` | C | OpenAI direct |
| **W8** | `review-with-claude-api.yml` | C | Anthropic direct |

---

# SEZIONE 1 — Organization Level

> Salta questa sezione se il repo e' su account **personale**. Vai alla [Sezione 2](#sezione-2--account--personal-level).

## 1.1 Licenze e subscription org-wide

- [ ] **Copilot Business o Enterprise attivo sull'organization**
  - **Dove**: `https://github.com/organizations/<ORG>/settings/copilot`
  - **Cosa sblocca**: tutta la piattaforma Copilot per i membri dell'org
  - **Richiesto da**: W5, W6 (Coding Agent richiede minimo Business; gli altri workflow Option B/C funzionano anche su Pro personale)

- [ ] **Policy "Copilot coding agent" su `Enabled` / `Allowed`**
  - **Dove**: `https://github.com/organizations/<ORG>/settings/copilot/policies` → sezione *"Coding agent"*
  - **Cosa sblocca**: i membri dell'org possono assegnare issue a `@copilot`
  - **Richiesto da**: W5, W6

- [ ] **Policy GitHub Models su `Enabled`** (se la tua org ha restrizioni custom)
  - **Dove**: `https://github.com/organizations/<ORG>/settings/copilot/policies` → sezione *"Models"*
  - **Cosa sblocca**: i workflow possono chiamare `https://models.github.ai`
  - **Richiesto da**: W1, W2, W3, W4

## 1.2 Actions policy org-wide

- [ ] **Allowed actions / reusable workflows** include `actions/checkout`, `actions/upload-artifact`, `actions/ai-inference`, `actions/create-github-app-token`
  - **Dove**: `Organization → Settings → Actions → General → Policies`
  - **Cosa sblocca**: i workflow del lab possono caricare queste action
  - **Richiesto da**: tutti (W1–W8). Se hai whitelist stretta, aggiungi anche `slackapi/slack-github-action` se aggiungi notifiche

- [ ] **Default workflow permissions** = `Read and write permissions`
  - **Dove**: `Organization → Settings → Actions → General → Workflow permissions`
  - **Cosa sblocca**: tetto massimo dei permessi che ogni workflow puo' richiedere via `permissions:` block
  - **Richiesto da**: tutti i workflow che scrivono (W1–W8)

## 1.3 Org-level secrets e variables (solo per pattern enterprise W6)

- [ ] **Org secret `REVIEW_APP_PRIVATE_KEY`** — contenuto del file `.pem` della GitHub App
  - **Dove**: `Organization → Settings → Secrets and variables → Actions → New organization secret`
  - **Visibility**: `Selected repositories` → i repo dove gira W6
  - **Cosa sblocca**: lo step `actions/create-github-app-token` puo' generare installation token al run-time
  - **Richiesto da**: W6 esclusivamente

- [ ] **Org variable `REVIEW_APP_ID`** — numero dell'App
  - **Dove**: stessa pagina, tab **Variables** → New organization variable
  - **Visibility**: `Selected repositories`
  - **Cosa sblocca**: il workflow sa quale App invocare
  - **Richiesto da**: W6 esclusivamente

- [ ] **Org secret `COPILOT_ASSIGN_PAT`** — fine-grained PAT del service account user
  - **Dove**: org secrets
  - **Visibility**: `Selected repositories`
  - **Cosa sblocca**: assegnamento di `@copilot` (App tokens sono rifiutati dall'API)
  - **Richiesto da**: W6 (consigliato), W5 (alternativamente puoi tenerlo come repo secret)

## 1.4 GitHub App installazione (solo per W6)

- [ ] **GitHub App creata e installata sull'org**
  - **Dove**: `Organization → Settings → Developer settings → GitHub Apps` → create
  - **Permissions**: `Contents:read`, `Issues:read+write`, `Pull requests:read`, `Metadata:read`
  - **Installation**: `Only select repositories` → i tuoi repo target
  - **Cosa sblocca**: token App con scope preciso per labels/comments
  - **Richiesto da**: W6 esclusivamente

## 1.5 Service account user (solo per W6)

- [ ] **Utente GitHub dedicato `<org>-review-bot` invitato come Member**
  - **Dove**: `Organization → People → Invite member`
  - **Cosa sblocca**: identita' "non-umana" autorizzata ad assegnare `@copilot`
  - **Richiesto da**: W6 (W5 puo' usare un PAT personale, ma per produzione enterprise usa il service account)

---

# SEZIONE 2 — Account / Personal Level

> Si applica sia ad account personali sia agli admin di un'org. Quello che fai qui vale per il tuo utente.

## 2.1 Subscription Copilot

- [ ] **Copilot Pro o Pro+ attivo** (personal) **o membro Business/Enterprise** (org)
  - **Dove**: `https://github.com/settings/copilot`
  - **Cosa sblocca**: accesso a GitHub Models, Coding Agent, model picker, ecc.
  - **Richiesto da**: W1, W2, W3, W4, W5, W6 (W7 e W8 funzionano anche senza Copilot perche' usano API esterne)

## 2.2 GitHub Models — accettazione terms (one-time)

- [ ] **Visitato [github.com/marketplace/models](https://github.com/marketplace/models) almeno una volta** e accettati i termini
  - **Dove**: link sopra
  - **Cosa sblocca**: il `GITHUB_TOKEN` del runner puo' chiamare `https://models.github.ai` senza HTTP 403
  - **Richiesto da**: W1, W2, W3, W4

## 2.3 Coding Agent abilitato sull'account

- [ ] **Verificare presenza della sezione *"Copilot coding agent"*** sulla pagina settings
  - **Dove**: `https://github.com/settings/copilot/coding_agent`
  - **Cosa sblocca**: ricevere assegnamenti di issue come `@copilot` (per gli admin org: anche permettere ai membri di farlo)
  - **Richiesto da**: W5, W6

## 2.4 Personal Access Token — solo per W5

- [ ] **Fine-grained PAT generato**
  - **Dove**: [github.com/settings/personal-access-tokens](https://github.com/settings/personal-access-tokens) → Generate new token (fine-grained)
  - **Configurazione**:
    - Resource owner: il tuo account (o org se member)
    - Repository access: `Only select repositories` → repo target
    - Repository permissions: `Issues: Read and write` + `Pull requests: Read`
    - Expiration: max 90 giorni
  - **Cosa sblocca**: assegnare `@copilot` da workflow (i token App sono rifiutati)
  - **Richiesto da**: W5

  > Per produzione: usa un service account dedicato (vedi Sezione 1.5), non un PAT personale.

---

# SEZIONE 3 — Repository Settings

> Tutti i workflow del lab girano su un singolo repository. Configura ogni voce su `https://github.com/<owner>/<repo>/settings`.

## 3.1 Actions abilitate

- [ ] **`Settings → Actions → General → Actions permissions`** = `Allow all actions and reusable workflows`
  - **Cosa sblocca**: le action di terze parti che usiamo possono caricare
  - **Richiesto da**: tutti (W1–W8)

  > Se vuoi essere piu' restrittivo: `Allow select non-GitHub actions` con allowlist `actions/checkout@*, actions/upload-artifact@*, actions/ai-inference@*, actions/create-github-app-token@*`.

## 3.2 Workflow permissions

- [ ] **`Settings → Actions → General → Workflow permissions`** = ☑ `Read and write permissions`
  - **Cosa sblocca**: tetto massimo dei permessi che i workflow possono dichiarare nel YAML
  - **Richiesto da**: tutti i workflow che scrivono qualcosa (W1–W8)

- [ ] **`Settings → Actions → General → Workflow permissions`** = ☑ `Allow GitHub Actions to create and approve pull requests`
  - **Cosa sblocca**: il Coding Agent (che gira come Actions sotto le quinte) puo' aprire la PR finale
  - **Richiesto da**: W5, W6

## 3.3 Repository secrets

Aggiungi questi su `Settings → Secrets and variables → Actions → tab "Secrets"`. Se l'hai gia' messo a livello org (Sezione 1.3), salta — un secret org-level e' visibile come repo secret.

| Secret name | Valore | Richiesto da |
|-------------|--------|-------------:|
| `COPILOT_ASSIGN_PAT` | Fine-grained PAT (Sezione 2.4 o service account 1.5) | W5, W6 |
| `REVIEW_APP_PRIVATE_KEY` | Contenuto `.pem` della GitHub App | W6 |
| `SFTP_HOST` | Hostname/IP server SFTP | W3 |
| `SFTP_USER` | Username SFTP | W3 |
| `SFTP_PASSWORD` | Password SFTP (alternativa a key) | W3 |
| `SFTP_PRIVATE_KEY` | Chiave SSH privata (alternativa a password) | W3 |
| `SFTP_REMOTE_PATH` | Path remoto base, es. `/uploads/reviews` | W3 |
| `SFTP_PORT` | Porta SFTP (default 22, opzionale) | W3 |
| `HTTP_UPLOAD_URL` | URL webhook.site o altro endpoint | W4 (opzionale — alternativo all'input workflow_dispatch) |
| `HTTP_UPLOAD_TOKEN` | Bearer token per HTTP endpoint | W4 (opzionale) |
| `OPENAI_API_KEY` | API key di [platform.openai.com/api-keys](https://platform.openai.com/api-keys) | W7 |
| `ANTHROPIC_API_KEY` | API key di [console.anthropic.com/settings/keys](https://console.anthropic.com/settings/keys) | W8 |

## 3.4 Repository variables

Su `Settings → Secrets and variables → Actions → tab "Variables"`:

| Variable name | Valore | Richiesto da |
|---------------|--------|-------------:|
| `REVIEW_APP_ID` | ID numerico della GitHub App | W6 |

> Variables differiscono dai Secrets perche' non sono mascherate nei log. App ID non e' sensibile.

## 3.5 Collaborator: bot Copilot (solo per W5, W6)

- [ ] **L'utente bot `Copilot` (o `copilot-swe-agent[bot]`) deve poter accedere al repo**
  - **Dove**: `Settings → Collaborators` (repo personale) o `Settings → Access → Manage access` (repo org)
  - **Cosa sblocca**: il Coding Agent ha le permission per leggere/scrivere quando viene assegnato a un'issue
  - **Auto-aggiunto** sui repo personali Pro/Pro+ con Coding Agent abilitato. Su org puo' richiedere aggiunta manuale.

---

# SEZIONE 4 — Workflow YAML — permissions block

> Questi permessi sono **gia' dichiarati nei file YAML del lab** — non devi cambiarli. Sono qui per riferimento, cosi' sai cosa fa ogni workflow.

## W1 — `scheduled-review.yml`

```yaml
permissions:
  contents: read       # checkout
  models: read         # GitHub Models API
  actions: write       # upload artifact
```

## W2 — `scheduled-review-curl.yml`

```yaml
permissions:
  contents: write      # postare commit comment
  models: read         # GitHub Models API
  actions: write       # upload artifact
```

## W3 — `review-and-upload-sftp.yml`

```yaml
permissions:
  contents: read
  models: read
  actions: write
```

## W4 — `review-and-upload-http.yml`

```yaml
permissions:
  contents: read
  models: read
  actions: write
```

## W5 — `scheduled-coding-agent-review.yml`

```yaml
permissions:
  contents: read       # checkout
  issues: write        # creare l'issue
  pull-requests: read  # ispezionare la PR aperta dall'agente
```

## W6 — `scheduled-coding-agent-review-app.yml`

```yaml
permissions: {}        # vuoto — usiamo solo i token che minted nel job
```

## W7 — `review-with-openai-api.yml`

```yaml
permissions:
  contents: read
  actions: write
```

## W8 — `review-with-claude-api.yml`

```yaml
permissions:
  contents: read
  actions: write
```

---

# SEZIONE 5 — Matrix di lookup: workflow → requisiti

| Requisito | W1 | W2 | W3 | W4 | W5 | W6 | W7 | W8 |
|-----------|:--:|:--:|:--:|:--:|:--:|:--:|:--:|:--:|
| Copilot Pro/Pro+/Business | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | – | – |
| GitHub Models terms accepted | ✓ | ✓ | ✓ | ✓ | – | – | – | – |
| Coding Agent enabled | – | – | – | – | ✓ | ✓ | – | – |
| Workflow permissions = Read and write | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| Allow GHA to create/approve PR | – | – | – | – | ✓ | ✓ | – | – |
| Secret `COPILOT_ASSIGN_PAT` | – | – | – | – | ✓ | ✓ | – | – |
| Variable `REVIEW_APP_ID` | – | – | – | – | – | ✓ | – | – |
| Secret `REVIEW_APP_PRIVATE_KEY` | – | – | – | – | – | ✓ | – | – |
| Secrets SFTP_* | – | – | ✓ | – | – | – | – | – |
| Secret `HTTP_UPLOAD_URL` / `HTTP_UPLOAD_TOKEN` | – | – | – | ✓* | – | – | – | – |
| Secret `OPENAI_API_KEY` | – | – | – | – | – | – | ✓ | – |
| Secret `ANTHROPIC_API_KEY` | – | – | – | – | – | – | – | ✓ |
| Bot Copilot collaborator | – | – | – | – | ✓ | ✓ | – | – |
| GitHub App installata sul repo | – | – | – | – | – | ✓ | – | – |
| Service account user nell'org | – | – | – | – | – | ✓ | – | – |

\* Per W4, in alternativa al secret puoi usare il workflow_dispatch input `webhook_url` (default lo trasforma in placeholder che fa failure intenzionale).

---

# SEZIONE 6 — Setup-paths per scenario

## Scenario A: PoC personale veloce (minimo indispensabile)

Vuoi provare il pattern. Funzionano W1, W2 (no commit comment), W7, W8.

- [ ] Sezione 2.1 — Copilot Pro/Pro+ attivo
- [ ] Sezione 2.2 — GitHub Models terms accepted
- [ ] Sezione 3.1 — Actions abilitate
- [ ] Sezione 3.2 — Workflow permissions "Read and write"
- [ ] Sezione 3.3 — `OPENAI_API_KEY` e/o `ANTHROPIC_API_KEY` se vuoi provare W7/W8

**Tempo setup**: ~10 minuti.

## Scenario B: Demo completa del lab (tutti i 6 workflow Option B + Coding Agent)

Vuoi mostrare tutti i pattern Option A + B in un ambiente personale.

Tutto di Scenario A, piu':

- [ ] Sezione 2.3 — Coding Agent abilitato
- [ ] Sezione 2.4 — Fine-grained PAT
- [ ] Sezione 3.2 — Spunta "Allow GHA to create and approve PR"
- [ ] Sezione 3.3 — Secret `COPILOT_ASSIGN_PAT`
- [ ] Sezione 3.5 — Bot Copilot collaborator (auto-aggiunto su Pro)
- [ ] Sezione 3.3 — Secrets SFTP_* + HTTP_UPLOAD_* (anche fake/placeholder webhook.site se vuoi solo testare)

**Tempo setup**: ~30 minuti.

## Scenario C: Deploy enterprise produzione (tutti gli 8 workflow)

Vuoi tutto, in produzione, su un'org.

Tutto di Scenario B, piu':

- [ ] Sezione 1 — tutto org-level
- [ ] Sezione 1.4 — GitHub App creata + installata
- [ ] Sezione 1.5 — Service account user
- [ ] Sezione 1.3 — Org secrets `REVIEW_APP_PRIVATE_KEY`, `COPILOT_ASSIGN_PAT`
- [ ] Sezione 1.3 — Org variable `REVIEW_APP_ID`

**Tempo setup**: ~2-4 ore (con automazione Terraform: vedi `ENTERPRISE-SETUP.md`).

---

# SEZIONE 7 — Diagnostica errori comuni

| Errore | Permission mancante | Fix rapido |
|--------|--------------------|------------|
| `403 Forbidden` su GitHub Models | `models: read` nel YAML O terms non accettati | Aggiungi `models: read` a `permissions:` + visita [marketplace/models](https://github.com/marketplace/models) |
| `403 Resource not accessible by integration` su issue create | Workflow permissions = Read only OR `issues: write` non dichiarato | Settings → Actions → Read and write + YAML `permissions: issues: write` |
| `GraphQL: Assigning agents is not supported with GitHub App installation tokens` | Stai usando `GITHUB_TOKEN` per assegnare `@copilot` | Crea PAT (Sezione 2.4) o service account (Sezione 1.5) + usa `COPILOT_ASSIGN_PAT` |
| `Resource not found` su `gh issue create --assignee @copilot` | Coding Agent non abilitato O bot non collaborator | Sezione 2.3 + Sezione 3.5 |
| `Cannot find model openai/gpt-5-mini` | Plan non include il modello O org policy lo blocca | Cambia in `gpt-4o-mini` o verifica plan |
| Secret expansion non funziona (`${{ secrets.X }}` empty) | Secret non esistente o visibility "Selected repositories" che non include questo repo | Verifica nome (case-sensitive) e visibility |
| `actions/ai-inference` exit code 1 con `model not allowed` | Org policy ha allowlist actions stretta | Sezione 1.2 — aggiungi `actions/ai-inference` |

---

# TL;DR

- **Personal**: 10 min di setup per workflow Option B/C basici, 30 min in totale per tutto il lab personale
- **Org enterprise**: 2-4 ore di setup, automatizzabile con Terraform tranne creazione App e service account
- **Most common error**: missing `models: read` nei workflow Option B + GitHub Models terms non accettati → vedi Sezione 2.2 e Sezione 4

Per qualsiasi workflow che fallisce: prima controlla la matrix Sezione 5, poi la Sezione 7 troubleshooting.
