# 3 Opzioni Agentiche per Code Review Schedulato — Confronto, Setup, Costi

> Documento di riferimento per scegliere l'architettura agentica corretta in base a vincoli di costo, controllo del modello, automazione richiesta. Aggiornato a maggio 2026 con il nuovo modello di billing token-based di GitHub Copilot.

---

## TL;DR — Tabella comparativa estesa

| Aspetto | A) Coding Agent | B) GitHub Models + Action | C) API LLM diretta |
|---------|:---:|:---:|:---:|
| **Modello selezionabile** | ❌ Solo "Auto" da workflow | ✅ Pinnato (gpt-5-mini, gpt-4o-mini, ecc.) | ✅ Qualunque modello del provider |
| **Costo prevedibile** | ❌ Variabile (Auto sceglie tra Sonnet/GPT-5) | ✅ Prevedibile, **free tier coperto** per uso piccolo | ✅ Prevedibile, controllo totale |
| **Schedulabile da workflow** | ⚠️ Si' ma con Auto | ✅ Si', cron + workflow_dispatch | ✅ Si', cron + workflow_dispatch |
| **Output tipico** | PR aperta dall'agente | File MD da `actions/ai-inference` | File MD da curl |
| **Audit trail** | Issue + PR su GitHub | Run history + artifact | Run history + artifact |
| **Multi-step reasoning** | ✅ Si' (l'agente itera) | ❌ Single-shot | ❌ Single-shot (a meno di scriversi il loop) |
| **Tool (read/edit/...)** | ✅ Gestiti dall'agent runtime | ❌ Solo input/output al modello | ❌ Solo input/output al modello |
| **Setup complessita'** | Bassa | Bassissima | Media (servono secret API esterni) |
| **Provider model** | OpenAI + Anthropic via Copilot | OpenAI via GitHub Models | OpenAI / Anthropic / Azure Foundry / qualunque |
| **Adatto a** | Bug fix, feature implementation, PR review interattiva | Audit, doc gen, classification, **task one-shot ricorrenti** | Tutto C + integrazione con foundry/Claude/modelli proprietari |

---

## Opzione A — GitHub Copilot Coding Agent

### Cosa e'

L'**agent server-side ufficiale** di GitHub Copilot. Triggerato assegnando un'issue all'utente bot `@copilot`. Gira in un runner Actions effimero gestito da GitHub, ha accesso al repo via `GITHUB_TOKEN` temporaneo, **chiude il lavoro aprendo una pull request** con i cambiamenti.

E' un "junior developer autonomo" — fa multi-step reasoning, puo' iterare, usare tool (read/edit/search/execute), gestire i propri commit.

### Setup (5 step)

**1. Verifica licenza:**
- Account personale: Copilot Pro o Pro+
- Org: Copilot Business o Enterprise + policy "Coding agent" su `Allowed`
- Su Copilot Free **non e' disponibile**

**2. Abilita policy a livello org** (solo Business/Enterprise):
- `https://github.com/organizations/<ORG>/settings/copilot/coding_agent`
- Imposta su `Enabled` per tutti o per repo selezionati

**3. Configura permessi Actions del repo:**
- `Settings → Actions → General → Workflow permissions`
- ✅ Read and write permissions
- ✅ Allow GitHub Actions to create and approve pull requests

**4. Scrivi un workflow che crea l'issue e la assegna a Copilot:**

```yaml
on:
  schedule:
    - cron: '0 6 * * 1'   # weekly
  workflow_dispatch:

permissions:
  contents: read
  issues: write

jobs:
  trigger:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - run: |
          gh issue create \
            --title "Weekly Code Review $(date +%F)" \
            --body-file .github/prompts/full-review.prompt.md \
            --assignee "@copilot" \
            --label "auto-review"
        env:
          GH_TOKEN: ${{ secrets.GITHUB_TOKEN }}
```

**5. Il Coding Agent prendera' in carico l'issue entro pochi minuti**, lavorera' su un branch effimero, aprira' una PR con i cambi (file in `out/` ad esempio).

### Costi

Modello di billing **token-based dal 1° giugno 2026**.

| Tier | Cosa include |
|------|--------------|
| Pro+ ($39/mese) | $39 in AI Credit/mese |
| Business ($19/utente/mese) | $19/utente in AI Credit/mese |
| Enterprise ($39/utente/mese) | $39/utente in AI Credit/mese |

Cost per run **dipende dal modello scelto da "Auto"**. Tipicamente:
- 400k LOC: ~$5-10/run (Auto sceglie un modello bilanciato)
- 50k LOC: ~$1-2/run

I credit AI sono **condivisi** con la chat IDE degli sviluppatori — somma il consumo del team.

### Pro / Contro

✅ Multi-step reasoning, tool use, output strutturato in PR
✅ Audit trail nativo (issue + PR)
✅ Setup minimo se hai gia' la policy attiva
❌ Modello non selezionabile da workflow
❌ Costi non prevedibili senza vincoli sull'Auto
❌ Non disponibile su Free

### Quando sceglierla

- Vuoi che l'agente faccia **fix proattivi** (apre PR con codice modificato), non solo report
- Hai budget AI Credits comodo e accetti il modello Auto
- Hai gia' Business/Enterprise con Coding Agent attivo
- Il caso d'uso e' **PR review interattiva** o **bug fixing schedulato**, non solo audit

### Schedulabile? Si', con caveat

Si schedula creando l'issue da cron come nello snippet sopra. Caveat:
- Il Coding Agent usera' "Auto" — non puoi forzare gpt-5-mini o altro
- La PR aperta resta aperta finche' qualcuno la merga/chiude
- Devi gestire la finestra "issue creata → PR aperta" (puo' variare 5-30 min)

---

## Opzione B — GitHub Models + `actions/ai-inference` (o curl)

### Cosa e'

**Chiamata diretta** a un modello LLM tramite l'**endpoint GitHub Models**, dentro un GitHub Action. Niente agent runtime, niente PR, niente issue. Solo: prompt → modello → file di output.

Il "modello agent" del lab2 (i file `.agent.md`) viene comunque usato — ma non e' interpretato dal Coding Agent runtime: e' letto dal workflow e passato come **system prompt** al modello.

### Setup (3 step)

**1. Verifica GitHub Models attivo:**
- Visita `https://github.com/marketplace/models`
- Se prima volta, accetta i terms di GitHub Models (one-time, gratuito)
- Disponibile su tutti i tier inclusi Pro/Pro+

**2. Workflow con `actions/ai-inference`:**

```yaml
on:
  schedule:
    - cron: '0 6 * * 1'
  workflow_dispatch:

permissions:
  contents: read
  models: read       # required for GitHub Models access
  actions: write

jobs:
  review:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: Bundle source
        run: |
          mkdir -p build out
          cat *.cs > build/source-bundle.txt

      - name: Run reviewer
        uses: actions/ai-inference@v1
        id: review
        with:
          model: openai/gpt-4o-mini
          system-prompt-file: .github/agents/code-reviewer.agent.md
          prompt-file: build/source-bundle.txt

      - name: Save report
        run: |
          cat > out/review.md <<'EOF'
          ${{ steps.review.outputs.response }}
          EOF

      - uses: actions/upload-artifact@v4
        with:
          name: reports
          path: out/
```

**3. Run** — `Actions → Run workflow`. Output disponibile come artifact.

> **Variante curl** (se l'action ha quirk con un modello specifico — vedi caso GPT-5 family con `max_completion_tokens`): vedi `ex-1/.github/workflows/scheduled-review-curl.yml` per template completo.

### Costi

**Free tier generoso** per l'uso individuale di sviluppo. Limiti effettivi (verifica sempre su [docs.github.com/billing](https://docs.github.com/en/billing)):

| Plan | Free tier GitHub Models |
|------|------------------------|
| Pro | Free tier limitato (~50 req/giorno per modelli low-tier) |
| Pro+ | Free tier piu' ampio + AI Credit ($39/mese) |
| Business | Free tier + AI Credit per utente |
| Enterprise | Free tier + AI Credit per utente |

**Per gpt-4o-mini / gpt-5-mini** (i modelli low-tier): per un workflow weekly che fa 2 chiamate (~5k input + ~1k output ciascuna), **rientri quasi sempre nel free tier** o nel buffer dei AI Credit inclusi senza costi extra.

Per modelli premium (gpt-4o, claude-3-5-sonnet) il consumo passa subito sui AI Credit e poi su overage:

| Modello | Cost per chiamata weekly (5k in + 1k out) |
|---------|------------------------------------------|
| gpt-4o-mini | ~$0.001 per chiamata (free tier copre) |
| gpt-5-mini | ~$0.003 per chiamata (free tier copre) |
| gpt-4o | ~$0.05 per chiamata |
| Sonnet 4.6 | ~$0.03 per chiamata |

### Pro / Contro

✅ Modello completamente selezionabile (`model: openai/gpt-4o-mini` ecc.)
✅ Costi nulli o trascurabili per uso piccolo (free tier)
✅ Setup ultra-minimo: 1 file YAML
✅ Nessun secret esterno richiesto (`GITHUB_TOKEN` basta)
❌ Single-shot — l'agent non itera, non usa tool, non apre PR
❌ Output deve essere parsato manualmente o convertito in PR/commit a mano
❌ Subject a quirk dell'action wrapper (es. caso `max_tokens` vs `max_completion_tokens`)

### Quando sceglierla

- Caso d'uso e' **audit / doc gen / classification** (one-shot)
- Vuoi controllo totale del modello e dei costi
- Non hai bisogno che l'agent apra PR autonomamente
- Setup deve essere veloce e zero-friction
- **Default raccomandato per il caso "review schedulata produce report"**

---

## Opzione C — API LLM diretta (OpenAI / Anthropic / Azure Foundry / altri)

### Cosa e'

Bypass totale di Copilot e GitHub Models. Il workflow chiama direttamente l'API del provider LLM via `curl`, usando una **API key** del provider salvata nei secret del repo.

Funziona con qualsiasi provider:
- **OpenAI** (api.openai.com) — gpt-4o, gpt-4o-mini, o3, ecc.
- **Anthropic** (api.anthropic.com) — Claude Opus, Sonnet, Haiku
- **Azure Foundry / Azure OpenAI** (modelli deployati nel tuo subscription)
- Modelli self-hosted (Ollama, vLLM, ecc.)

### Setup (4 step)

**1. Ottieni API key dal provider scelto:**
- OpenAI: `https://platform.openai.com/api-keys` — nuova chiave, copia il valore
- Anthropic: `https://console.anthropic.com/settings/keys`
- Azure Foundry: deployment-specific endpoint + key dal portale Azure

**2. Aggiungi la key come secret del repo:**
- `Settings → Secrets and variables → Actions → New repository secret`
- Nome: `OPENAI_API_KEY` / `ANTHROPIC_API_KEY` / `AZURE_OPENAI_KEY`
- Value: la key copiata

**3. Workflow con `curl`:**

**Esempio OpenAI:**

```yaml
- name: Run reviewer (OpenAI)
  env:
    OPENAI_API_KEY: ${{ secrets.OPENAI_API_KEY }}
  run: |
    PAYLOAD=$(jq -n \
      --arg sys "$(cat .github/agents/code-reviewer.agent.md)" \
      --arg usr "$(cat build/source-bundle.txt)" \
      '{
         model: "gpt-4o-mini",
         messages: [
           {role: "system", content: $sys},
           {role: "user",   content: $usr}
         ],
         max_completion_tokens: 1500
       }')

    curl -sS -X POST https://api.openai.com/v1/chat/completions \
      -H "Authorization: Bearer ${OPENAI_API_KEY}" \
      -H "Content-Type: application/json" \
      -d "${PAYLOAD}" \
      | jq -r '.choices[0].message.content' > out/review.md
```

**Esempio Anthropic Claude:**

```yaml
- name: Run reviewer (Claude)
  env:
    ANTHROPIC_API_KEY: ${{ secrets.ANTHROPIC_API_KEY }}
  run: |
    PAYLOAD=$(jq -n \
      --arg sys "$(cat .github/agents/code-reviewer.agent.md)" \
      --arg usr "$(cat build/source-bundle.txt)" \
      '{
         model: "claude-3-5-sonnet-20241022",
         max_tokens: 1500,
         system: $sys,
         messages: [
           {role: "user", content: $usr}
         ]
       }')

    curl -sS -X POST https://api.anthropic.com/v1/messages \
      -H "x-api-key: ${ANTHROPIC_API_KEY}" \
      -H "anthropic-version: 2023-06-01" \
      -H "Content-Type: application/json" \
      -d "${PAYLOAD}" \
      | jq -r '.content[0].text' > out/review.md
```

**Esempio Azure Foundry (modello deployato in Azure):**

```yaml
- name: Run reviewer (Azure Foundry)
  env:
    AZURE_OPENAI_KEY: ${{ secrets.AZURE_OPENAI_KEY }}
    AZURE_ENDPOINT: ${{ vars.AZURE_OPENAI_ENDPOINT }}     # es: https://my-deployment.openai.azure.com
    AZURE_DEPLOYMENT: ${{ vars.AZURE_DEPLOYMENT_NAME }}   # es: gpt-4o-mini-prod
    AZURE_API_VERSION: '2024-08-01-preview'
  run: |
    PAYLOAD=$(jq -n \
      --arg sys "$(cat .github/agents/code-reviewer.agent.md)" \
      --arg usr "$(cat build/source-bundle.txt)" \
      '{
         messages: [
           {role: "system", content: $sys},
           {role: "user",   content: $usr}
         ],
         max_completion_tokens: 1500
       }')

    curl -sS -X POST "${AZURE_ENDPOINT}/openai/deployments/${AZURE_DEPLOYMENT}/chat/completions?api-version=${AZURE_API_VERSION}" \
      -H "api-key: ${AZURE_OPENAI_KEY}" \
      -H "Content-Type: application/json" \
      -d "${PAYLOAD}" \
      | jq -r '.choices[0].message.content' > out/review.md
```

**4. Run** — come opzione B, il workflow gira su cron / dispatch e produce file in `out/`.

### Costi

Pagati **direttamente al provider**, NON dentro i Copilot AI Credit:

| Provider | Modello | Input $/1M | Output $/1M | Per chiamata weekly |
|----------|---------|-----------:|------------:|--------------------:|
| OpenAI | gpt-4o-mini | $0.15 | $0.60 | ~$0.001 |
| OpenAI | gpt-4o | $2.50 | $10.00 | ~$0.025 |
| OpenAI | o3-mini | $1.10 | $4.40 | ~$0.011 |
| Anthropic | Claude Haiku 3.5 | $0.80 | $4.00 | ~$0.008 |
| Anthropic | Claude Sonnet 4.6 | $3.00 | $15.00 | ~$0.030 |
| Anthropic | Claude Opus 4.7 | $5.00 | $25.00 | ~$0.060 |
| Azure | (uguale a modello base) | (uguale) | (uguale) | (idem) |

Vantaggio costi rispetto a Copilot AI Credit:
- **Niente moltiplicatori Copilot** (Opus 4.7 in Copilot ha 27× moltiplicatore — diretto via Anthropic costa il prezzo "vero")
- **Quota per provider**, non condivisa con la chat dei dev
- **OIDC federation** disponibile (Azure) → niente API key statiche

### Pro / Contro

✅ Modello completamente libero — anche modelli non in Copilot (Claude Opus, Gemini, modelli proprietari)
✅ Foundry / self-hosted / OpenAI compatible API
✅ Costi diretti senza moltiplicatori Copilot
✅ Quota separata dal team
❌ Serve API key in secret (gestione, rotazione, audit)
❌ Lavoro extra: ogni provider ha schema JSON diverso
❌ Nessun automatic free tier — paghi ogni token

### Quando sceglierla

- Vuoi usare un modello **non disponibile in Copilot** (es. Claude Opus 4.7 puro)
- Hai gia' un'enterprise agreement con OpenAI / Anthropic / Azure
- Vuoi pagare **al prezzo "vero"** del provider, non ai prezzi Copilot
- Hai un modello su **Azure Foundry** (deployato nel tuo subscription)
- Vuoi self-hosted o cross-provider routing (LiteLLM, Ollama, ecc.)

---

## Monitoraggio costi — dove guardare

### Per Coding Agent + GitHub Models (Opzioni A + B)

**Account personale:**
- `https://github.com/settings/billing/plans_and_usage`
- Sezione *"GitHub Copilot"* → vedi consumo AI Credit del mese, residuo, overage attuale
- Sezione *"GitHub Actions"* → minuti runner consumati (rilevante se repo privato)
- Sezione *"GitHub Models"* → free tier residuo del mese

**Account org/Enterprise:**
- `https://github.com/organizations/<ORG>/settings/billing/plans_and_usage`
- Stessi sezioni, aggregate per tutti gli utenti
- Filtro per repo / per utente nel dropdown

**Dashboard utili:**
- *"Spending limits"* nella stessa pagina — imposta budget cap mensile per evitare overage non pianificati
- *"Usage report"* (csv export) per audit dettagliato

### Per API LLM diretta (Opzione C)

Ogni provider ha la sua dashboard separata:

| Provider | URL dashboard | Cosa monitori |
|----------|---------------|---------------|
| OpenAI | `https://platform.openai.com/usage` | Token input/output/totale per modello, $ |
| Anthropic | `https://console.anthropic.com/usage` | Stesso |
| Azure Foundry | `Azure Portal → Cost Management + Billing` | Aggregato Azure Subscription |
| Self-hosted | n/a | Solo costo infra (server GPU, ecc.) |

> **Pratica raccomandata**: dopo le prime 2-3 run reali, **annota il consumo effettivo** sulla dashboard del provider e confronta con la stima. Da qui calibri la cadenza (weekly vs monthly) o cambi modello.

---

## Aggiungere upload SFTP / HTTP a `ex-1/`

Il template `ex-1/` produce solo artifact GitHub Actions. Per spedire i file fuori (SFTP / HTTP esterno) aggiungi questi step **dopo** lo step `Save code-review output` e **prima** dello `Upload reports as artifact`.

### Setup secrets (una tantum)

Vai su `repo → Settings → Secrets and variables → Actions` e aggiungi:

**Per SFTP:**
- `SFTP_HOST` (hostname)
- `SFTP_USER` (username)
- `SFTP_PASSWORD` *o* `SFTP_PRIVATE_KEY` (contenuto chiave SSH)
- `SFTP_REMOTE_PATH` (es. `/uploads/reviews`)

**Per HTTP:**
- `HTTP_UPLOAD_URL` (es. `https://compliance.acme.io/api/upload`)
- `HTTP_UPLOAD_TOKEN` (Bearer token)

### Snippet SFTP (con `lftp`)

```yaml
- name: Install lftp
  run: |
    sudo apt-get update -qq
    sudo apt-get install -y -qq lftp

- name: Configure SSH key (if private key is used)
  if: env.SFTP_PRIVATE_KEY != ''
  env:
    SFTP_PRIVATE_KEY: ${{ secrets.SFTP_PRIVATE_KEY }}
  run: |
    mkdir -p ~/.ssh
    echo "$SFTP_PRIVATE_KEY" > ~/.ssh/id_sftp
    chmod 600 ~/.ssh/id_sftp

- name: Upload to SFTP
  env:
    SFTP_HOST: ${{ secrets.SFTP_HOST }}
    SFTP_USER: ${{ secrets.SFTP_USER }}
    SFTP_PASSWORD: ${{ secrets.SFTP_PASSWORD }}
    SFTP_REMOTE_PATH: ${{ secrets.SFTP_REMOTE_PATH }}
  run: |
    REMOTE_DIR="${SFTP_REMOTE_PATH%/}/${{ github.repository }}/$(date -u +'%Y-%m-%d')"

    if [ -f ~/.ssh/id_sftp ]; then
      AUTH="-u ${SFTP_USER}, --key-file ~/.ssh/id_sftp"
    else
      AUTH="-u ${SFTP_USER},${SFTP_PASSWORD}"
    fi

    lftp ${AUTH} "sftp://${SFTP_HOST}" <<EOF
    set sftp:auto-confirm yes
    mkdir -p "${REMOTE_DIR}"
    cd "${REMOTE_DIR}"
    mput out/*.md
    bye
    EOF
```

### Snippet HTTP (con `curl`)

```yaml
- name: Upload to HTTP endpoint
  env:
    HTTP_UPLOAD_URL: ${{ secrets.HTTP_UPLOAD_URL }}
    HTTP_UPLOAD_TOKEN: ${{ secrets.HTTP_UPLOAD_TOKEN }}
  run: |
    REVIEW_DATE=$(date -u +'%Y-%m-%d')

    for f in out/*.md; do
      NAME=$(basename "$f")
      echo "Uploading $NAME..."
      curl --fail -sS -X POST "${HTTP_UPLOAD_URL}" \
        -H "Authorization: Bearer ${HTTP_UPLOAD_TOKEN}" \
        -F "repo=${{ github.repository }}" \
        -F "review_date=${REVIEW_DATE}" \
        -F "file_name=${NAME}" \
        -F "file=@${f}"
    done
```

### Posizione nel workflow

Inserisci entrambi gli snippet **dopo** `Save doc-writer output` e **prima** di `Upload reports as artifact`. Se vuoi solo SFTP o solo HTTP, includi un solo blocco. Per averli alternativi, usa una env var `UPLOAD_MODE: sftp | http` con `if: env.UPLOAD_MODE == '...'` su ogni step.

> Riferimento completo con switch SFTP/HTTP gia' integrato: `repo-template/.github/workflows/02-publish-review.yml` (template del flusso a due workflow del lab session-4).

---

## FAQ — risposte alle domande chiave

### 1. Posso usare il Coding Agent per code review e schedulare?

**Si', con limiti.** L'agent supporta task di review — basta scrivere un prompt nel body dell'issue tipo *"Read all C# files and produce a code review report"*. Per schedulare, usi un workflow cron che crea l'issue e la assegna a Copilot:

```yaml
- run: gh issue create --assignee "@copilot" --body-file .github/prompts/review.prompt.md
```

**Limite chiave**: il Coding Agent gira con il modello **"Auto"** scelto da GitHub al momento dell'esecuzione. Non puoi forzare gpt-5-mini o altro da workflow. Se ti basta accettare Auto, e' la via piu' integrata. Se vuoi modello fisso, **opzione B o C**.

### 2. Si puo' selezionare il modello per il Coding Agent?

**Solo dalla UI, per-issue.** Quando assegni manualmente un'issue a `@copilot` dalla UI di GitHub.com, appare un picker con: Auto, Claude Sonnet 4.5, Claude Opus 4.5/4.6, GPT-5.2/5.3/5.4-Codex.

**Da workflow non si puo' specificare** — c'e' una [feature request aperta](https://github.com/github/gh-aw/issues/16294) ma non ancora implementata.

**Workaround**: nel workflow #1 crei l'issue assegnata a `@copilot` con Auto (default), poi NOTIFICHI un operatore (Slack/Teams) → l'operatore va sulla UI dell'issue, ri-assegna scegliendo il modello voluto, l'agent riparte. Aggiunge step manuale ma da' controllo del modello.

### 3. Dove capisco che sto usando `actions/ai-inference` vs GitHub Agentic Workflow?

Sono **due cose diverse, spesso confuse**:

| | `actions/ai-inference` | GitHub Agentic Workflows (`gh-aw`) |
|---|------------------------|-----------------------------------|
| Cos'e' | Una **GitHub Action** (action marketplace) | Un **framework / CLI extension** per definire workflow agentici in markdown |
| Maturita' | Stable, generalmente disponibile | In evoluzione, alcune feature in preview |
| Come la riconosci | `uses: actions/ai-inference@v1` nel YAML | `gh aw` come comando, file `.github/agents/*.md` con direttive specifiche `gh-aw` |
| Cosa fa | Una singola chiamata al modello via GitHub Models | Orchestrazione di piu' agent, tool, MCP, in flow definiti in MD |
| Quando usarla | Task one-shot semplici | Flow agentici complessi (multi-agent, tool use, memory) |

**Nel `ex-1/` di questo lab usiamo `actions/ai-inference`** — riconoscibile dalla riga `uses: actions/ai-inference@v1` nel workflow. Non stai usando `gh-aw` (sarebbe un layer sopra, con file di config diversi e CLI dedicato).

Se in futuro `gh-aw` matura ed espone parametri come `--model`, potresti migrare per avere multi-agent + tool use mantenendo modello fisso. Per ora, `actions/ai-inference` e' lo strumento giusto per task singoli.

### 4. Posso usare modelli su Azure Foundry / OpenAI direct / Claude direct / licenza esterna?

**Si', tutti — tramite l'Opzione C** (API LLM diretta).

| Provider / Source | Funziona? | Come |
|-------------------|:---:|------|
| OpenAI direct (api.openai.com) | ✅ | Secret `OPENAI_API_KEY`, curl a `/v1/chat/completions` |
| Anthropic Claude direct (api.anthropic.com) | ✅ | Secret `ANTHROPIC_API_KEY`, curl a `/v1/messages` (formato diverso da OpenAI!) |
| Azure OpenAI / Azure AI Foundry | ✅ | Secret `AZURE_OPENAI_KEY` + endpoint deploy-specific. **OIDC federation** disponibile per evitare key statiche |
| Modelli self-hosted (Ollama, vLLM, ecc.) | ✅ | Solo se l'endpoint e' raggiungibile dal runner Actions (VPN o pubblico) |
| Google Gemini direct | ✅ | Secret `GEMINI_API_KEY`, formato proprio |
| Modelli "private" tramite Foundry | ✅ | Endpoint del deployment specifico — vedi snippet Azure sopra |
| AWS Bedrock | ✅ | AWS credentials in secret + AWS CLI/SDK nel runner |

**Vincolo principale**: **NON puoi usare il Coding Agent (Opzione A) con modelli esterni** — il Coding Agent gira solo sui modelli del catalog Copilot. Per modelli esterni, devi passare per Opzione B (limitata al catalog GitHub Models) o Opzione C (libera).

**Note importanti per Foundry/Enterprise:**
- Azure OpenAI ti permette **DLP / data residency** (i dati non lasciano la tua subscription Azure)
- OIDC federation tra GitHub Actions e Azure → niente key statiche, audit migliore
- Rate limit / quota per deployment, non condivisi con altri tenant

**Quando vale la pena Opzione C invece di B:**
- Devi rispettare data residency (i tuoi dati non possono andare a OpenAI USA → usa Azure region UE)
- Hai gia' un contratto/quota con un provider e vuoi sfruttarlo
- Vuoi un modello specifico non in GitHub Models (Claude Opus 4.7 puro senza moltiplicatori Copilot, Gemini 2.5, modelli aperti deployati)
- Vuoi switch dinamico tra provider (LiteLLM proxy, ecc.)

---

## Quale scegliere — decision tree

```
                            ┌─ Vuoi che l'agente APRA PR con codice modificato?
                            │
                            ├─── SI'  → A) Coding Agent
                            │           (accetti Auto, hai Business/Enterprise)
                            │
                            ├─── NO   → produce solo report MD?
                                            │
                                            ├─ SI', e voglio modello free/cheap (gpt-4o-mini, gpt-5-mini)?
                                            │       │
                                            │       └─→ B) GitHub Models + actions/ai-inference  ★ default
                                            │
                                            ├─ SI', ma serve modello esterno (Claude Opus, Foundry, ...)?
                                            │       │
                                            │       └─→ C) API LLM diretta (curl + secret API key)
                                            │
                                            └─ Vuoi multi-agent + tool use + memory persistente?
                                                    │
                                                    └─→ Considera GitHub Agentic Workflows (gh-aw)
                                                        oppure framework esterno (LangGraph, AutoGen, ecc.)
```

**Per il caso "review schedulata weekly su 400k LOC C# che produce MD report":** **Opzione B** con `gpt-4o-mini` o `gpt-5-mini`. Costo trascurabile, setup zero-friction, full automation. E' il pattern del `ex-1/` di questo lab.

**Per il caso "vogliamo Claude Opus per accuracy massima":** **Opzione C** con Anthropic API direct. Costo ~$0.06/run, qualita' top.

**Per il caso "vogliamo che apra PR con fix automatici":** **Opzione A** (Coding Agent). Accetti Auto, hai gia' Business/Enterprise.
