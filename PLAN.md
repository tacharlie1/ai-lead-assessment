# D&E AI Adoption Plan

**Prepared by:** Thabang Charlie, AI Lead candidate

**For:** Head of D&E

**Planning Horizon:** 12 months **Scope:** about 60+ .NET developers, plus QA and BA teams

## Summary

I would move D&E from today's AI-assisted work (Copilot completion, unmeasured) to an **AI-Native SDLC**, in stages, without compromising security. The plan rests on three commitments:

1. **Start where risk is low and value is measurable:** code review assist and developer tasks (tests, refactors, bug fixes).
2. **Earn security approval with a complete, phased pack,** and make the safe behaviour the default through tooling, not policing.
3. **Measure from day one against a baseline,** with speed and quality reported together, so every expansion decision rests on data.

**Assumptions** (to be validated in the first 30 days)
- Copilot is licensed and used unevenly. Nobody has measured it.
- Code and customer data may not leave the approved boundary until security signs off. Claude approval may take 2–4 months, so the plan must deliver value before then.
- Budget covers a pilot of roughly 15–20% of developers. Wider rollout needs a business case from pilot data.
- Legacy C++ is out of scope for year one. Tooling is weaker and the risk is higher.
- All numbers in this plan are proposed targets and estimates, not promised outcomes. The baseline and pilot will refine them.

---

# 1a. AI-Native SDLC  Roadmap

## North star
**Within 12 months, AI assists every lifecycle stage under human review, and agentic workflows (AI plans, edits, runs tests, opens a PR) are standard for well-scoped work such as bug fixes, tests, refactors and migrations.**

This is a staged move toward an **AI-Native SDLC**: a lifecycle designed around AI taking part in every stage (planning, design, build, test, review, release, maintenance), not AI tools added on top of a process built for people. In that model the human role shifts from writing code to defining intent, constraints and testable specifications, then reviewing the result. Each stage hands the next a written artifact (a spec, a test, a plan), so the loop depends on clear specs and fast feedback. That is why the plan builds tests, review and standards before agents.

I chose it because autocomplete gains are small and hard to see, while task-level automation produces measurable gains. Once code is cheap to produce, review and QA become the bottleneck, so they are targets too. Every change still gets a human reviewer, and every agent runs with least privilege.

## Where to start
1. **Code review assist.** Advisory only, so low risk. It addresses a real bottleneck and works on today's tools. The Part 2 pilot is a first version.
2. **Developer tasks** (tests, refactors, bug fixes). Easy to verify with the test suite and easy to measure.
3. **QA** (test case and test data generation), from month 3. Needs shared standards for a "good" test first.
4. **BA / requirements** (story drafting, acceptance criteria, handoff checks), from month 4. Run as one small experiment.
5. **Agentic multi-step work**, from month 6, once guardrails and metrics exist.

## Milestones
**First 90 days**
- **Days 0–30, assess and baseline.** Interview 8–10 people across dev, QA and BA. Audit Copilot usage and map the lifecycle for bottlenecks. Capture baseline metrics (see 1c). Publish a one-page usage policy and an approved-tools register. Pick a pilot team and champions. Draft the security pack. Form a small **AI steering group** (Head of D&E, security, legal, engineering and QA leads) to prioritise use cases and unblock approvals.
- **Days 31–60, pilot on approved tools.** Structured Copilot use cases on the pilot team. Trial the local PR review assistant on a few repos. Submit the security pack. First workshops.
- **Days 61–90, prove and prepare.** First results against baseline. Start a prompt and pattern library. Design the Claude Code pilot and its exit criteria.

**Months 4–12**
- **4–6:** Claude Code approved for the pilot team (sandboxed, managed settings). Two more teams adopt the playbook. QA use cases start. First ROI report.
- **10–12:** Access for everyone who wants it. First agentic CI workflows (automated review, test generation). Champions run training. Year-two plan, including C++.

## Build vs. buy vs. SaaS
I assess every tool with five questions, in this order:

1. **Does it differentiate us?** If not, don't build it.
2. **Can it pass security?** A tool security won't approve has no value.
3. **Does it fit our stack?** Source control, CI and .NET.
4. **What is the 12-month total cost,** including upkeep?
5. **How easy is it to leave?** Avoid lock-in.

**Default stance**
- **Buy** commodity capability, such as coding assistants.
- **Build** only thin glue we own (review scripts, prompt library, dashboards), behind swappable interfaces, as in Part 2.
- **SaaS** only where it never sees code or customer data.

---

# 1b. Copilot to Claude Code under strict security

## Getting through approval
Slow approvals usually come from missing information, so I give security one complete pack: a data flow diagram (what leaves the machine and where it is stored), vendor terms (no training on our data, retention, sub-processors, SOC 2 / ISO 27001), the access model (SSO, audit logs, centrally managed settings), and a phased proposal. The proposal is a time-boxed pilot with exit criteria, so security approves an experiment, not a permanent commitment.

| Expected risk | Mitigation |
|---|---|
| Code or secrets leaving the boundary | Enterprise terms, secret scanning, managed deny rules for `.env`, keys and customer data folders |
| Prompt injection from repo content or web pages | No network by default, tool allow-lists, human approval for shell commands |
| Destructive agent actions | Sandbox or dev container, no production credentials on laptops, branch protection on `main` |

## Usage standards
1. Approved tools and repos only (kept in the approved-tools register). No customer data, credentials or production data in prompts.
2. The author owns every line committed, whoever wrote it. Reviewers apply the same bar.
3. AI-assisted PRs carry a label. No penalty for using AI or for not using it.
4. Agents read and edit within the repo by default. Shell and network need approval. Never bypass permissions.
5. AI-written code needs tests, and a human reviews the tests.
6. Each repo keeps a maintained `CLAUDE.md` (build, test, style, banned patterns).

**Making sure they are followed:** use tooling, not policing. Managed settings make the policy the default. CI enforces secret scanning, SAST and the PR label. Branch protection covers agent-authored changes too. A monthly audit-log review looks at patterns, not individuals. Champions report where a standard is impractical, because standards people work around need fixing.

## Rollout and enablement
1. **Pilot team (8–10 people)** with a mix of seniority, a QA engineer and a supportive lead. Run 6–8 weeks.
2. **Training:** two hands-on workshops on the team's own code (prompting and context, reviewing AI output critically, safe use), recorded short demos, fortnightly sharing sessions.
3. **Knowledge transfer:** one champion per ~8 developers, a shared library of prompts, skills and `CLAUDE.md` examples, a wins-and-failures channel, twice-weekly office hours. Visible peer use drives adoption, so champions demo in team meetings.
4. **Waves** of two or three teams, each starting from the pilot playbook.
5. **Stepping out** by months 9–12: champions run training, the playbook lives in the repo, and I keep ownership of standards, metrics and the vendor relationship.

## Copilot in the transition
Keep it until Claude Code is approved and proven. Removing it early costs productivity and spoils the baseline. Pilot developers keep both so we can compare on the same work. Habits learned now (context, review, testing) carry over. Decide around months 7–9 on cost, usage and quality: replace it, or keep it for inline completion only. Check the renewal date now and avoid a long commitment before then.

---

# 1c. Measuring adoption, value, quality and cost

| Category | Metrics |
|---|---|
| **Adoption** | Seats activated vs. licensed, weekly active users, share using it 3+ days a week, spread across teams |
| **Usage** | Requests per active user, share of PRs labelled AI-assisted, use cases used, suggestion acceptance rate |
| **Value** | PR cycle time, lead time, time to first review, throughput with PR size alongside, monthly survey of time saved, QA test authoring time, BA tickets returned for clarification |
| **Quality** | Change failure rate, post release defects, rework and reverts, coverage and mutation score on AI-assisted code, SAST findings, developer trust score |
| **Cost** | Cost per active user, cost per merged PR, token spend by team and use case, training and support time, net value |

The headline numbers are the four DORA metrics. Leadership already knows them and they are harder to game than activity counts.

**Baseline and ROI**
- Pull 3–6 months of history from source control and CI before the pilot, and run one baseline survey.
- Compare the pilot team with its own history and with a similar team that has not started. Acknowledge the limits: small samples and novelty effects.
- ROI = hours saved × loaded hourly cost − licences, usage and enablement. Discount self-reported savings by 30–50%. Show a conservative and an expected case.
- If speed rises but change failure rate or rework rises too, the net result is not positive, so I report them together.
- Report quarterly on one page: numbers, lessons, decision needed.

**Helping struggling teams.** I use the numbers to find where support is needed, not to rank people.
- *Low adoption:* ask why (no time, unclear policy, bad first experience, no `CLAUDE.md`), then pair and tailor the setup.
- *High usage, no cycle-time gain:* look at where work waits. Often review is the bottleneck, so add AI-assisted review and smaller PRs.
- *High usage, rising defects or rework:* the team accepts output too readily. Add test requirements and run a session on reviewing AI output.
- *High cost, low value:* find wasteful patterns (huge contexts, repeated retries) and share efficient ones from other teams.
- *Strong teams:* capture their practices and fold them into the playbook.