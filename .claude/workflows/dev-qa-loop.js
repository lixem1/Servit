export const meta = {
  name: 'servit-dev-qa-loop',
  description: 'Plan a goal into tasks, then implement + QA each one sequentially (dev -> QA -> one fix round). Does NOT deploy; production is human-gated.',
  whenToUse: 'When you have a real backlog/goal for Servit and want the developer+QA agents to work through it automatically.',
  phases: [
    { title: 'Plan', detail: 'supervisor decomposes the goal into tasks' },
    { title: 'Build', detail: 'developer implements + QA verifies each task' },
  ],
}

const goal = typeof args === 'string' ? args : (args && args.goal)
if (!goal) throw new Error('Pasa el objetivo del ciclo como args (string), p. ej. "Agregar pantalla de historial de solicitudes".')

const TASKS_SCHEMA = {
  type: 'object',
  properties: {
    tasks: {
      type: 'array',
      items: {
        type: 'object',
        properties: {
          title: { type: 'string' },
          description: { type: 'string' },
          area: { type: 'string', enum: ['mobile', 'backend', 'both'] },
          acceptance: { type: 'string' },
        },
        required: ['title', 'description', 'area', 'acceptance'],
      },
    },
  },
  required: ['tasks'],
}

const DEV_SCHEMA = {
  type: 'object',
  properties: {
    branch: { type: 'string' },
    summary: { type: 'string' },
    filesChanged: { type: 'array', items: { type: 'string' } },
    buildOk: { type: 'boolean' },
  },
  required: ['branch', 'summary', 'buildOk'],
}

const QA_SCHEMA = {
  type: 'object',
  properties: {
    passed: { type: 'boolean' },
    findings: {
      type: 'array',
      items: {
        type: 'object',
        properties: {
          severity: { type: 'string', enum: ['blocker', 'major', 'minor'] },
          area: { type: 'string' },
          description: { type: 'string' },
          repro: { type: 'string' },
          expected: { type: 'string' },
          actual: { type: 'string' },
        },
        required: ['severity', 'description'],
      },
    },
    manualChecklist: { type: 'array', items: { type: 'string' } },
  },
  required: ['passed', 'findings'],
}

phase('Plan')
const plan = await agent(
  `Objetivo a planificar para Servit:\n\n${goal}\n\nLee PROJECT_STATUS.md y descomponlo en tareas pequeñas e independientemente testeables.`,
  { agentType: 'supervisor', model: 'claude-sonnet-5', phase: 'Plan', label: 'plan', schema: TASKS_SCHEMA },
)

let tasks = (plan && plan.tasks) || []
const CAP = 8
if (tasks.length > CAP) {
  log(`El plan tiene ${tasks.length} tareas; proceso las primeras ${CAP} (las demás quedan para otra corrida).`)
  tasks = tasks.slice(0, CAP)
}
log(`${tasks.length} tarea(s) planificada(s).`)

const results = []
for (let i = 0; i < tasks.length; i++) {
  const t = tasks[i]
  const p = `Task ${i + 1}: ${t.title}`
  phase(p)

  const brief = `Tarea: ${t.title}\nÁrea: ${t.area}\nDescripción: ${t.description}\nCriterios de aceptación: ${t.acceptance}`

  let dev = await agent(
    `${brief}\n\nImpleméntala en una rama agent/<slug>. Verifica flutter analyze y dotnet build antes de terminar.`,
    { agentType: 'developer', model: 'claude-opus-4-8', phase: p, label: `dev:${t.title}`, schema: DEV_SCHEMA },
  )

  let qa = await agent(
    `${brief}\n\nEl developer trabajó en la rama "${dev && dev.branch}". ${dev && dev.summary}\n\nHaz checkout de esa rama y verifícala: flutter analyze, dotnet build, e integration tests en el simulador iOS para los flujos afectados. Reporta hallazgos y un checklist manual.`,
    { agentType: 'qa', model: 'claude-sonnet-5', phase: p, label: `qa:${t.title}`, schema: QA_SCHEMA },
  )

  if (qa && qa.passed === false && (qa.findings || []).length) {
    const findingsText = (qa.findings || [])
      .map((f, n) => `${n + 1}. [${f.severity}] ${f.description}${f.repro ? ' | repro: ' + f.repro : ''}`)
      .join('\n')
    dev = await agent(
      `${brief}\n\nQA encontró problemas en la rama "${dev && dev.branch}":\n${findingsText}\n\nCorrígelos en la misma rama y vuelve a verificar los gates.`,
      { agentType: 'developer', model: 'claude-opus-4-8', phase: p, label: `dev-fix:${t.title}`, schema: DEV_SCHEMA },
    )
    qa = await agent(
      `${brief}\n\nEl developer aplicó correcciones en "${dev && dev.branch}". Vuelve a verificar en el simulador iOS y confirma si pasa.`,
      { agentType: 'qa', model: 'claude-sonnet-5', phase: p, label: `qa-retry:${t.title}`, schema: QA_SCHEMA },
    )
  }

  results.push({ task: t, dev, qa })
}

return {
  goal,
  note: 'Release NO se ejecutó. El deploy a producción (VM + iPhones) requiere aprobación humana explícita — pídelo al agente release cuando estés listo.',
  tasks: results.map((r) => ({
    title: r.task.title,
    area: r.task.area,
    branch: r.dev && r.dev.branch,
    buildOk: r.dev && r.dev.buildOk,
    qaPassed: r.qa && r.qa.passed,
    openFindings: (r.qa && r.qa.findings) || [],
    manualChecklist: (r.qa && r.qa.manualChecklist) || [],
  })),
}
