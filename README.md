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
| Workflow fail su `actions/ai-inference` con `403` | GitHub Models non abilitato sull'account | Vai su [marketplace/models](https://github.com/marketplace/models) e accetta i terms |
| `model not found: openai/gpt-5-mini` | Modello non disponibile nel catalog del tuo plan | Edita `MODEL` in `scheduled-review.yml` a `openai/gpt-4o-mini` (sempre disponibile) |
| Output vuoto in `out/code-review.md` | `max-tokens` troppo basso o prompt malformato | Aumenta `max-tokens` a 2500, controlla i log dello step `Run code-reviewer` |
| `permissions` error | Repo permette solo read | Settings → Actions → Read and write permissions |
| Artifact non appare | Step `Save code-review output` fallito | Controlla i log: spesso e' un escape problem nel `cat <<EOF` |

## Costi

GitHub Models con `gpt-5-mini`:
- Input ~3-5k token per run (3 file C# bundle)
- Output ~1k token per run (review breve)
- Costo per run: trascurabile, **rientra nel free tier mensile** di GitHub Models per la maggior parte degli account

Su free tier puoi fare ~50-100 run/mese senza pagare nulla. Weekly = 4 run/mese ≪ limit.

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
