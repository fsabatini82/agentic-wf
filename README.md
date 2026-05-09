# ex-1 — Esempio "easy": code review + doc generator schedulati con gpt-5-mini

> Esempio minimale per testare un workflow GitHub Actions schedulato che usa **modello fissato** (gpt-5-mini), senza dipendere dal Coding Agent (che non permette di scegliere il modello).

## Cosa fa

Una volta deployato in un repo GitHub, ogni lunedi' alle 06:00 UTC il workflow:

1. Legge tutti i file `.cs` del repo
2. Li passa a un agente **code-reviewer** (system prompt definito in `.github/agents/code-reviewer.agent.md`) → produce `out/code-review.md`
3. Li passa a un agente **doc-writer** (system prompt in `.github/agents/doc-writer.agent.md`) → produce `out/code-doc.md`
4. Pubblica i due file come **artifact** scaricabili dalla pagina della run su GitHub Actions

Tutto usando **`actions/ai-inference`** + **GitHub Models** con modello pinnato a `openai/gpt-5-mini`. Niente Coding Agent, niente issue/PR ceremony.

## Perche' questa architettura (non Coding Agent)

| Approccio | Modello selezionabile? | Costo prevedibile? | Setup |
|-----------|:---:|:---:|------|
| Coding Agent (`gh issue create --assignee @copilot`) | ❌ Sempre "Auto" | ❌ Variabile | Semplice |
| **GitHub Models + `actions/ai-inference`** ← *qui* | ✅ Pinnato a gpt-5-mini | ✅ Prevedibile | Semplice |
| OpenAI API diretta (curl) | ✅ Qualsiasi | ✅ Prevedibile | Serve OPENAI_API_KEY |

Per "voglio gpt-5-mini schedulato in automatico", **GitHub Models** e' la via. Usa `GITHUB_TOKEN` (auto-provided), non servono secret esterni.

## Struttura

```
ex-1/
├── README.md                              ← questo file
├── SampleApp.csproj
├── Program.cs                             ← uses EmployeeService, has 1-2 smells
├── EmployeeService.cs                     ← ~95 righe, 8+ smells volutamente piantati
├── .gitignore
├── out/                                   ← reports generati dalla run (gitignored)
│   └── .gitkeep
└── .github/
    ├── copilot-instructions.md            ← contesto progetto
    ├── agents/
    │   ├── code-reviewer.agent.md         ← persona reviewer (max 8 finding)
    │   └── doc-writer.agent.md            ← persona doc (max 80 righe)
    └── workflows/
        └── scheduled-review.yml           ← cron + workflow_dispatch
```

I "smells" piantati nel codice C#:
- `Console.WriteLine` invece di `ILogger`
- SQL via interpolazione (`$"... WHERE Department = '{department}'"`)
- Empty catch block che swallowa exception
- Nested if/else 4 livelli in `CreateEmployee`
- Magic strings `"ACTIVE"`, `"IT"`, `"SALES"`, `"PENDING"`
- Metodo privato dead (`LegacyBonusV1` mai chiamato)
- Field non letto (`_retryAttempts`)
- `ToList().Where().FirstOrDefault()` invece di `.FirstOrDefault()` diretto
- Logica duplicata calcolo bonus

L'obiettivo: il code-reviewer deve pescarne almeno 5-6.

---

# Guida passo-passo

## Prerequisiti

- Account GitHub con **Copilot Pro / Pro+ / Business / Enterprise** (per usare GitHub Models)
- Repo GitHub dove pushare questo esempio (puo' essere personale, pubblico o privato)
- .NET 9 SDK in locale se vuoi anche compilare il sample (opzionale)

> Nota su GitHub Models: include una **free tier mensile** che copre tranquillamente questo esempio (input ~5k token, output ~1.5k token per run × 4 run/mese ≪ free tier limit). In molti casi **non paghi nulla** per girarlo.

## Step 1 — Crea un nuovo repo GitHub

1. Vai su `https://github.com/new`
2. Nome: ad es. `copilot-review-demo`
3. Visibility: **Private** (o Public, indifferente per il funzionamento)
4. **Non** inizializzare con README/license/gitignore (ci sono gia' nel template)
5. Crea il repo. Copia l'URL.

## Step 2 — Inizializza locale e push

Da dentro la folder `ex-1/` di questo lab:

```bash
cd c:/Dev/demo-sdd-lab/session-4-gh-review-agents/ex-1

# Inizializza repo locale
git init -b main
git add .
git commit -m "feat: scheduled code review + doc generator (gpt-5-mini)"

# Collega remote e push
git remote add origin https://github.com/<TUO-USER>/copilot-review-demo.git
git push -u origin main
```

## Step 3 — Configura permessi Actions

Sul repo appena pushato, vai su:

`Settings → Actions → General → Workflow permissions`

- ✅ **Read and write permissions**

(Per questo esempio non serve "Allow GitHub Actions to create and approve pull requests" perche' non aprono PR — pubblicano solo artifact.)

Salva.

## Step 4 — Verifica che GitHub Models sia abilitato

Su `https://github.com/settings/copilot` → cerca la sezione *"GitHub Models"*. Deve essere **Enabled** o disponibile per il tuo account.

Se non lo vedi: vai su `https://github.com/marketplace/models` e accetta i termini di GitHub Models una volta sola.

## Step 5 — Lancia il workflow a mano

1. Repo → tab **Actions**
2. Sidebar → **Scheduled Code Review (gpt-5-mini)**
3. Click **Run workflow** → branch `main` → lascia `run_doc_writer: true` → **Run workflow**
4. Aspetta 30-90 secondi che la run termini (e' veloce con gpt-5-mini)

## Step 6 — Verifica l'output

A run completata:

1. Click sulla run nella lista
2. Sezione **Summary** in cima → vedi un preview del code-review
3. Sezione **Artifacts** in fondo → click su `review-reports-<run_id>` per scaricare uno zip con `out/code-review.md` e `out/code-doc.md`

Apri i due file localmente. Esempio output atteso:

**out/code-review.md** (atteso):
```markdown
# Code Review — 2026-05-08

## Summary
- HIGH: 2 | MEDIUM: 4 | LOW: 1

## Findings

### [HIGH] SQL injection via string interpolation
- **File**: `EmployeeService.cs:93`
- **Issue**: User input is interpolated directly into a SQL string.
- **Fix**: Use parameterised queries / EF Core LINQ with bound parameters.

### [HIGH] Exception swallowed by empty catch
- **File**: `EmployeeService.cs:76`
- **Issue**: catch block silently discards all exceptions, hiding failures.
- **Fix**: Catch specific exception types and log via ILogger before rethrowing.

(... 4-6 more findings ...)

## Top 3 actions
1. Replace string-interpolated SQL with parameterised queries.
2. Remove the empty catch block; let unexpected exceptions propagate.
3. Replace Console.WriteLine with ILogger<T>.
```

**out/code-doc.md** (atteso): un single-page markdown con Overview, Architecture, Class Reference (3 classi), Setup commands, un esempio di usage in 5-10 righe.

## Step 7 — Schedule weekly attivo

Niente da fare. Il workflow e' gia' configurato `cron: '0 6 * * 1'` (lunedi' 06:00 UTC). Andra' in automatico la prossima settimana.

Per cambiare cadenza: edita `.github/workflows/scheduled-review.yml` riga `- cron: '0 6 * * 1'` con il tuo cron. Esempi:
- `0 6 * * *` — ogni giorno alle 06:00 UTC
- `0 6 1 * *` — il primo del mese
- `0 6 * * 1,4` — lunedi' e giovedi'

---

## Troubleshooting

| Problema | Causa | Fix |
|----------|-------|-----|
| `400 Unsupported parameter: 'max_tokens' is not supported with this model. Use 'max_completion_tokens' instead.` | Modelli famiglia **GPT-5** (gpt-5-mini, gpt-5-3-codex, ecc.) hanno cambiato API: vogliono `max_completion_tokens`. `actions/ai-inference@v1` invia ancora il legacy `max_tokens`. | Due opzioni: **(A)** usa `openai/gpt-4o-mini` come `MODEL` (cambiato di default in questo template — stessa fascia costo/qualita', funziona). **(B)** rimuovi del tutto l'input `max-tokens:` dall'action (gia' fatto in questo template). Quando `actions/ai-inference` supportera' GPT-5, potrai tornare a `gpt-5-mini`. |
| Workflow fail su `actions/ai-inference` con `403` | GitHub Models non abilitato sull'account | Vai su [marketplace/models](https://github.com/marketplace/models) e accetta i terms |
| `model not found: openai/<modello>` | Modello non disponibile nel catalog del tuo plan | Edita `MODEL` in `scheduled-review.yml` con un modello disponibile — `openai/gpt-4o-mini` e' il piu' affidabile |
| Output vuoto in `out/code-review.md` | Prompt malformato o quota esaurita | Controlla i log dello step `Run code-reviewer`; se output e' troncato, aggiungi `max-tokens: 2500` (solo per modelli non GPT-5) |
| `permissions` error | Repo permette solo read | Settings → Actions → Read and write permissions |
| Artifact non appare | Step `Save code-review output` fallito | Controlla i log: spesso e' un escape problem nel `cat <<EOF` |

## Costi

GitHub Models con `gpt-5-mini`:
- Input ~3-5k token per run (3 file C# bundle)
- Output ~1k token per run (review breve)
- Costo per run: trascurabile, **rientra nel free tier mensile** di GitHub Models per la maggior parte degli account

Su free tier puoi fare ~50-100 run/mese senza pagare nulla. Weekly = 4 run/mese ≪ limit.

## Due varianti del workflow

In `.github/workflows/` ci sono **due workflow** che fanno la stessa cosa con strategie diverse:

| File | Approccio | Modello | Quando usarla |
|------|-----------|---------|---------------|
| `scheduled-review.yml` | `actions/ai-inference@v1` (action ufficiale) | `openai/gpt-5-mini` con `max-tokens` rimosso | **Default**: setup minimo, manutenzione zero |
| `scheduled-review-curl.yml` | `curl` diretto sull'endpoint GitHub Models | `openai/gpt-5-mini` con controllo completo del JSON body | **Fallback**: se la prima fallisce con errori `max_tokens` o vuoi pinnare `max_completion_tokens`, temperatura, ecc. |

**La variante curl** (Variant B) e' utile perche':
- Manda direttamente `max_completion_tokens` (nuovo campo richiesto da GPT-5 family) — niente quirk dell'action wrapper
- Permette di settare anche `temperature`, `top_p`, `response_format` — controllo completo
- Funziona con qualsiasi modello del catalog GitHub Models
- Nessuna dipendenza da action di terze parti che potrebbero cambiare schema

Lo svantaggio: e' piu' codice (~30 righe vs 5 con l'action). Se l'action funziona, usala. Se no, switcha alla curl.

**Per attivare solo una delle due** (evitare doppia run schedulata):
- Mantieni `on: schedule` solo nel file che vuoi attivo
- Nell'altro file commenta o elimina il blocco `schedule:`
- Oppure rinomina con estensione diversa (es. `.yml.disabled`)

## Estensioni naturali

Una volta che il base funziona, puoi:

1. **Aggiungere upload SFTP/HTTP** ai report (vedi il workflow `02-publish-review.yml` del template `repo-template/` di session-4)
2. **Aggiungere un terzo agente** (es. `arch-reviewer`) replicando il pattern: nuovo file in `agents/` + nuovo step nel workflow
3. **Commit & PR**: invece di artifact, fai committare i file in `out/` su un branch e aprire una PR — utile per audit trail in repo
4. **Cambiare modello per task diverso**: code-review con gpt-5-mini, doc-writer con gpt-5-3-codex (qualita' superiore per la doc) — basta avere due env var diverse e settare `model:` per step

## Differenze rispetto al Coding Agent (per discussione col cliente)

| Aspetto | Coding Agent | Questo workflow (GitHub Models) |
|---------|--------------|---------------------------------|
| Model selection | Solo "Auto" da workflow | **Pinnato a piacere** |
| Costo | Variabile (Sonnet/Opus selezionati da Auto) | Prevedibile (gpt-5-mini ≈ free tier) |
| Output | PR aperta dall'agente | File in `out/` come artifact / commit |
| Audit trail | Issue + PR su GitHub | Run history + artifact |
| "Agent personality" | YAML in `.agent.md` riconosciuto nativamente | Lo stesso file e' usato come system prompt |
| Tool (read/edit/...) | Gestiti dall'agent runtime | Non disponibili — solo input/output al modello |
| Multi-step reasoning | Si' (l'agente puo' fare iterazioni) | No — single shot per step |
| Adatto a | Bug fix, feature implementation, PR review interattive | **Audit, doc generation, classification — task one-shot** |

In sintesi: il Coding Agent e' un "junior dev" che lavora in autonomia. Questo workflow e' uno "static analyzer LLM-based" — meno potente, ma costo controllato e modello deterministico. Per audit schedulati e' di solito quello che serve.

## FAQ

Dove monitorare i costi

Account personale:

https://github.com/settings/billing/plans_and_usage → sezione GitHub Copilot (AI Credit), GitHub Actions (minuti runner), GitHub Models (free tier residuo)

Imposta uno spending limit dalla stessa pagina per non avere sorprese
Account org:

https://github.com/organizations/<ORG>/settings/billing/plans_and_usage — stessa struttura, aggregata

Per gpt-5-mini specificamente: con un workflow weekly da 2 chiamate (~5k input + ~1k output ciascuna), rientri quasi sempre nel free tier o nel buffer di AI Credit inclusi. Pratica: dopo le prime 2-3 run guarda la dashboard, vedrai un consumo trascurabile (frazione di centesimo per run). Per il setup ex-1 come configurato, costo atteso ≈ $0.

Push report verso API esterna o SFTP

Nel doc trovi gli snippet pronti (sezione "Aggiungere upload SFTP / HTTP a ex-1/"). Sintesi:

SFTP (con lftp):

Aggiungi 4 secrets: SFTP_HOST, SFTP_USER, SFTP_PASSWORD o SFTP_PRIVATE_KEY, SFTP_REMOTE_PATH
Inserisci 3 step nel workflow: install lftp → configura key (se SSH key) → mput out/*.md su path strutturato per repo+data
Testa con lftp da locale prima
HTTP (con curl):

Aggiungi 2 secrets: HTTP_UPLOAD_URL, HTTP_UPLOAD_TOKEN
Inserisci 1 step nel workflow: loop sui file, curl -X POST -F "file=@..."
Testa con curl da locale prima
Entrambi gli step vanno dopo Save code-review output e prima di Upload reports as artifact. Riferimento completo (con switch SFTP|HTTP gia' integrato): repo-template/.github/workflows/02-publish-review.yml del lab.

Chiarimenti:

1. Coding Agent per review + scheduling
Sì, fattibile schedulando un cron che crea l'issue assegnata a @copilot. Vincolo: gira sempre con modello "Auto" — non puoi forzare gpt-5-mini da workflow.

2. Selezione modello nel Coding Agent
Solo dalla UI quando assegni manualmente l'issue (picker con Auto/Sonnet/Opus/GPT-5.x). Da workflow non si può, feature request aperta ma non ancora implementata. Workaround: notify-and-reassign manuale.

3. actions/ai-inference vs GitHub Agentic Workflows

Sono due cose diverse:

actions/ai-inference@v1: action marketplace, una chiamata one-shot al modello via GitHub Models. È quello che usa il nostro esempio.

GitHub Agentic Workflows (gh-aw): framework superiore per orchestrare multi-agent + tool + MCP in flow markdown. In evoluzione, alcune feature ancora in preview.

Lo riconosci dalla riga uses: actions/ai-inference@v1 nel YAML. Se vedessi gh aw run ... o file con direttive gh-aw specifiche, saresti sull'altro.

4. Foundry / OpenAI direct / Claude direct / modelli esterni

Tutti supportati tramite Opzione C (API LLM diretta con curl). Nel doc trovi snippet pronti per:

OpenAI direct (api.openai.com) — secret OPENAI_API_KEY
Anthropic Claude direct (api.anthropic.com) — formato leggermente diverso da OpenAI
Azure OpenAI / Foundry — secret + endpoint deployment-specific, supporta OIDC federation per evitare key statiche
Vincolo importante: il Coding Agent (Opzione A) NON supporta modelli esterni — gira solo sul catalog Copilot. Per Claude Opus puro, Foundry, Gemini, ecc., devi passare da Opzione C.

RECAP OPZIONE A (CODING AGENT) VS OPZIONE B (AI-INFERENCE) VS OPZIONE C (API DIRETTA)

Vantaggio Opzione C su B: niente moltiplicatori Copilot (Opus 4.7 in Copilot ha moltiplicatore 27× — diretto via Anthropic costa il prezzo "vero"), quota separata dal team, possibile data residency su Azure region UE.

Caratteristiche dei due nuovi workflow

Tratti comuni:

Stesso flusso review (bundle → strip frontmatter → ai-inference × 2) di scheduled-review.yml
gpt-5-mini come default (con fallback gpt-4o-mini documentato)
Schedule commentato — solo workflow_dispatch per la demo (decommenti il cron: quando vai in produzione)
Artifact backup sempre upload-ato (if: always()) — anche se l'upload esterno fallisce, hai i file scaricabili dalla run
Job summary con preview del code-review
Verifica esistenza secrets prima di tentare l'upload (fail-fast con messaggio chiaro)
Specifiche SFTP (review-and-upload-sftp.yml):

Supporto sia password sia chiave SSH (rileva quale è impostata)
Path remoto strutturato: ${SFTP_REMOTE_PATH}/<owner>/<repo>/<YYYY-MM-DD> — ogni run finisce in cartella distinta
Pre-trust dell'host (ssh-keyscan) per evitare prompt interattivi con chiave SSH
Porta configurabile (default 22)
Secrets richiesti: SFTP_HOST, SFTP_USER, SFTP_PASSWORD o SFTP_PRIVATE_KEY, SFTP_REMOTE_PATH, opzionale SFTP_PORT
Specifiche HTTP (review-and-upload-http.yml):

Multipart/form-data con campi: repo, review_date, file_name, file
Bearer token nell'header Authorization
Loop sui file con tracking di success/error count
Input dry_run: se selezionato a true non invia la POST ma logga cosa farebbe — utile per la demo per mostrare il flusso senza dipendere da un endpoint reale
Fail del job se almeno un file fallisce (con esito riportato nel summary)
Secrets richiesti: HTTP_UPLOAD_URL, HTTP_UPLOAD_TOKEN


DEMO LIVE:

Per SFTP:

Imposta i 4 secrets in Settings → Secrets and variables → Actions (puoi usare un server SFTP demo tipo sftpcloud.io per ottenere credenziali temporanee gratis)
Actions → Review + Upload via SFTP (manual demo) → Run workflow
La run gira in 30-60s, mostra l'upload nei log, e committa i file artifact come backup

Per HTTP:

Senza endpoint reale: vai su webhook.site, copia l'URL univoco, settalo come HTTP_UPLOAD_URL, metti un token finto come HTTP_UPLOAD_TOKEN. La pagina webhook.site mostrerà la POST in arrivo con headers e file in tempo reale — ottimo per dimostrare il flusso ai partecipanti

Actions → Review + Upload via HTTP REST (manual demo) → Run workflow
Lascia dry_run: false per la demo "vera" oppure dry_run: true se vuoi solo mostrare il flusso senza endpoint configurato