# Crime Empire — Review Ledger

The compact authority on commit-review coverage, review outcomes, and the current verification
baseline. It carries no active scope and grants no permission; `CURRENT_MILESTONE.md` is the only
active handoff.

Read this before reviewing a commit, then read that commit's milestone archive and the canon listed
in `AGENTS.md`. Detailed review narratives belong in `docs/milestones/`, not here. The full ledger as
it stood before the 2026-09-12 process normalization remains recoverable with:

```powershell
git show c7dd34b:docs/REVIEW_LEDGER.md
```

## Operating rules

`AGENTS.md` defines the implementer/reviewer loop and Class A/B/C depth. This ledger adds the rules
needed to keep coverage honest:

1. Work oldest unresolved first inside the active range. Every review names the exact isolated diff.
2. A later commit never substitutes for an earlier one. A correction that passes does not imply its
   unreviewed parent was reviewed.
3. Record reviewer identity and whether the reviewer authored any inspected commit. Self-review is
   preflight evidence, not independent review.
4. Test-green is not review; PASS is not owner acceptance; owner acceptance is not evidence that an
   independent review occurred. Record each separately.
5. `STATUS NOT ESTABLISHED` means the repository does not preserve enough evidence to claim a review
   outcome. It does not mean PASS, FAIL, or definitely never reviewed.
6. A grouped row is one coverage record for every exact hash it names. Never split it in a way that
   changes the shared outcome.
7. Class is assigned from the diff when the review occurs. Historical rows are not retroactively
   assigned a class from their commit messages.
8. A tracked ledger cannot cover the commit that contains its own update. Fold that outcome into the
   next independently authorized change. Never create a commit solely to record its own review or the
   review of the immediately preceding bookkeeping commit.

P1/P2 findings require Matt's ruling and, when accepted, a correction. A NOTE is non-blocking and
does not start a correction loop. Repeated notes are surfaced for a human ruling; repetition alone
does not turn them into defects.

## Coverage boundary and queue

**Reconciled checkpoint: `303eed0`.** Every commit through exact
`303eed0f1504c76c59671821877be9a98ed82b5e` appears below, either alone or in an exact group. Coverage
through `c7dd34b` preserves the pre-cleanup ledger and milestone evidence; the subsequent rows record
the cleanup, correction chain, 2026-09-12 historical audit, its independently reviewed repairs, the
Milestone 027 authorization transition, and the reviewed implementation.

The previous authoritative checkpoint was `824f3fc`. Rows at or before it marked
`STATUS NOT ESTABLISHED` are preserved legacy uncertainty and do not reopen closed milestones unless
Matt explicitly asks. The **active ordered range begins after `824f3fc`**. Astra's independent static
audit at `891368d` established the outcomes of its 29 requested rows (38 unique commits), while
preserving every grouped row. Astra subsequently reviewed the remaining exact commits in
chronological order, including `891368d` itself and the final correction record `8651ecd`. Matt
accepted both PASS outcomes on 2026-09-12. The historical chronological review gate through
`8651ecd` is closed. Astra then independently passed `608f05e`, and Matt accepted that transition
before implementation began. Claude independently failed `64881a1` on one accepted P2 record defect;
the runtime implementation had no blocking finding. Matt then closed milestone 027 on 2026-09-12,
exempting the record correction `303eed0` from a further review round by owner ruling; that row is an
owner closure and not an independent review. This ledger update cannot establish the review outcome
of the commit that contains it.

The audit was an independent exact-diff review by Astra, who authored none of the inspected commits.
It was static: Astra did not run builds, tests, Runner verification, Godot, restart, or
mutation checks. Matt accepted the audit findings on 2026-09-12. Its overall verdict was FAIL because
three defects remained live at audited HEAD `891368d`; their focused corrections are `dfefbc1`,
`bdcaab1`, and `8fdf2e5`. Astra passed the first two, failed the third, and then passed its correction
`86eef5e`; Matt accepted the correction state. The separate audit-record correction chain ended at
`8651ecd`, which Astra passed and Matt accepted. Historical FAIL outcomes remain FAIL even where a
later commit corrected the defect.

## Legacy coverage through `824f3fc`

Detailed findings are in the named milestone archives or the pre-normalization ledger above.

| Commit(s) | Established outcome |
|---|---|
| `46f0777` | Reviewed in the reorganization pass; decision-ID defect fixed by `4030699` (`milestones/001`). |
| `4030699`, `65a97c4` | **STATUS NOT ESTABLISHED.** |
| `7032981` | Codex reviewed, no findings; accepted (`milestones/002`). |
| `5463157` | **STATUS NOT ESTABLISHED.** |
| `097fbda` | Codex FAIL, five findings; corrected by `cf22e5d`. |
| `cf22e5d` | Codex FAIL, three findings; corrected by `2a74a5d`. |
| `2a74a5d` | Codex FAIL, three findings; corrected by `f97ef76`. |
| `f97ef76` | Codex FAIL, three findings; corrected by `b8fe921`. |
| `2d9177d` | **STATUS NOT ESTABLISHED.** |
| `b8fe921` | Codex FAIL, two P1s; corrected by `e83dacf`. Was falsely recorded as verified at the time. |
| `b3c404b` | **STATUS NOT ESTABLISHED.** |
| `d142582` | Codex reviewed with findings; corrected by `fb2c84d`. |
| `fb2c84d` | Codex FAIL; findings not preserved and later retired by Matt as superseded, not fixed. |
| `e83dacf` | Initially skipped, later Codex FAIL with three findings; corrected by `cbadb0d` and `170991b`. |
| `a5a72f1` | Codex found a false verification; withdrawn by `d2af4c8`. |
| `714fbc3` | Codex FAIL, three P1s; corrected by `c828bfa`. |
| `11c4a4a`, `d2af4c8` | **STATUS NOT ESTABLISHED.** |
| `cbadb0d` | Codex reviewed, no code findings. |
| `170991b` | Codex reviewed, no code findings; one documentation finding fixed by `d685015`. |
| `d685015` | Codex PASS; Matt accepted milestone 003. |
| `2893cf1` | **STATUS NOT ESTABLISHED.** |
| `dac4362` | Explicitly not reviewed; reconciled as an ordered-review checkpoint. Its false automation claim was later corrected. |
| `c828bfa` | Codex FAIL, five findings; corrected by `d783745`. |
| `d783745` | Codex FAIL, two findings; corrected by `612bd50`. |
| `612bd50` | Codex FAIL, two findings; corrected by `1fe8a15`. |
| `1fe8a15` | Codex PASS; Matt accepted milestone 004. |
| `20f82bd` | Codex FAIL; corrected by `6cbc385`. |
| `6cbc385` | Codex FAIL; corrected by `9703d83`. |
| `9703d83` | Codex PASS; Matt accepted the documentation correction. |
| `cdbcff1` | Codex FAIL, three documentation findings; corrected by `221b5cf`. |
| `221b5cf` | Codex PASS; Matt accepted. |
| `2e895a5` | **STATUS NOT ESTABLISHED.** |
| `f942871` | Codex FAIL, five findings; corrected by `90ff97c` (`milestones/005`). |
| `90ff97c` | Codex FAIL, one documentation finding; corrected by `5e2adc1`. |
| `5e2adc1` | Codex FAIL, one stale-status finding; corrected by `711553c`. |
| `711553c` | **STATUS NOT ESTABLISHED.** |
| `1fe5b9a` | Codex FAIL, six findings; corrected by `3ddd8a1` (`milestones/006`). |
| `3ddd8a1` | Codex confirmed the six fixes and found five more; corrected by `404b416`. |
| `404b416` | Codex PASS; Matt accepted milestone 006. |
| `6355347` | Codex FAIL: rewrote an append-only archive; corrected through `6ba0737`/`b8e5ed4`. |
| `974a88a` | Codex found one scope breach; Matt accepted a bounded fixture exception and closed milestone 007. |
| `46a5651` | Codex FAIL: corrected history by rewriting an append-only header; corrected by `1c6889f`. |
| `1c6889f` | Codex FAIL: falsely recorded `46a5651` as unreviewed; corrected by `53e912e`. |
| `53e912e` | Codex PASS; Matt accepted. |
| `6ba0737` | Codex FAIL, three documentation findings; corrected by `b8e5ed4`. |
| `b8e5ed4` | Codex PASS; Matt accepted. |
| `7a9773b` | Codex FAIL, one finding; corrected by `9a29342` (`milestones/008`). |
| `9a29342` | Codex FAIL, two findings including false hashes; corrected by `7e0700e`. |
| `7e0700e` | Codex PASS; Matt accepted milestone 008. |
| `3f08685` | Codex FAIL, one false-superlative finding; corrected later. |
| `901d345` | Codex FAIL, three findings; corrected by `b4900aa` (`milestones/009`). |
| `b4900aa` | Codex confirmed fixes and found one P1; corrected by `c447a23`. |
| `c447a23` | Codex FAIL, two findings; corrected by `49b71a6`. |
| `49b71a6` | Codex found two documentation/comment findings; corrected by `0f52d75`. |
| `0f52d75` | Codex PASS. |
| `c0bb60f` | Not reviewed by Codex; covered by Matt's later self-review-based acceptance. |
| `7ca7819` | Claude self-review only; Matt accepted milestone 009 on that disclosed weaker basis. |
| `12d1054` | **STATUS NOT ESTABLISHED.** |
| `824f3fc` | Claude self-review only; Matt accepted milestone 010 on that disclosed weaker basis. |

## Active-range coverage after `824f3fc`

Rows are chronological. `UNRESOLVED` means the row remains in the ordered queue. The reviewer assigns
its A/B/C class only after opening the exact diff.

| Commit(s) | Review evidence and disposition |
|---|---|
| `bec0370`, `22e73d1`, `925611a` | Astra independently reviewed all three together (Class B): FAIL, three P2s; Matt accepted the findings. False claims: only two added numeric literals (six additional occurrences actually exist); milestone 010 first to need no corrective round (milestone 002, `7032981`, is a counterexample); every row from `c0bb60f` onward accepted and self-reviewed (the same diff labels `12d1054` status not established). No existing scoring coefficient was retuned. Corrections `da43fbb` and `bad7ad4` each received independent Class B FAIL; `0482dfc` received independent Class B PASS, accepted by Matt on 2026-09-12, closing this correction chain. Original FAIL remains the outcome for these three hashes. Preserve as one grouped review. Full append-only history: [milestone 010](milestones/010-a-denial-that-can-win.md). |
| `6a8a765` | Claude self-reviewed; later Codex FAIL. Corrected by `3c86ba4`; Matt accepted the correction (`milestones/011`). |
| `40f0ded`, `520924b`, `3004d2f`, `c7ae3d6` | Astra independent historical audit (Class B): **FAIL**. The claim that `e83dacf` was permanently skipped contradicted its later recorded review and correction; normalized by `d6af1a6`. Preserve as one grouped review (`milestones/011`, `milestones/012`). |
| `3871d23`, `58016e8`, `10c42c3` | Astra independent historical audit (Class A): **FAIL**. Repeated the same false permanent-skip history; no separate blocking defect found in the added test logic. Preserve as one grouped review (`milestones/012`). |
| `c637092` | Delayed Codex review returned milestone-012 findings; corrected by `3c86ba4`; Matt accepted the correction (`milestones/012`). |
| `f6d3c91`, `52e3252`, `6af2b5f` | Astra independent historical audit (Class B): **PASS WITH NOTES**. Design and scope stayed within authority; historical measurements were not rerun. Preserve as one grouped review (`milestones/013`). |
| `3c86ba4` | Codex PASS; Matt accepted corrections to milestones 011–012. |
| `0ec0c95`, `1046704` | Astra independent historical audit (Class B): **PASS**. Agent/skill instructions and narrowed milestone-013 accounting authorization preserved deferred mutation scope. Preserve as one grouped review (`milestones/013`). |
| `a0c6be8` | Codex FAIL, four findings; corrected by `af6e90e` (`milestones/013`). |
| `af6e90e` | Codex FAIL, one further P2; corrected by `a75a54e`. |
| `a75a54e` | Codex PASS; Matt accepted milestone 013. |
| `712a125` | Codex FAIL, four findings; corrected by `556f2b2` (`milestones/014`). |
| `556f2b2` | Codex FAIL, two further P2s; corrected by `ff4213a`. |
| `ff4213a` | Astra independent historical audit (Class A): **PASS WITH NOTES**. Cash and rendered-information corrections inspected statically; runtime verification was not rerun. Matt had already accepted milestone 014 (`milestones/014`). |
| `9537b38` | Codex FAIL, four findings; corrected by `af7d34f` (`milestones/015`). |
| `af7d34f` | Codex FAIL, one residual P1; corrected by `bc79425`. |
| `bc79425` | Astra independent historical audit (Class A): **PASS WITH NOTES**. The reflection-based persistence fingerprint fixes the omission. Non-blocking archive count wording remains inaccurate. Matt had already accepted milestone 015 (`milestones/015`). |
| `66917c7` | Codex FAIL, four findings; corrected by `380a241` (`milestones/016`). |
| `380a241` | Codex FAIL, one new P1; corrected by `809fe60`. |
| `809fe60` | Claude self-reviewed, then Codex PASS; Matt accepted milestone 016. |
| `4c65f34` | Astra independent historical audit (Class B): **FAIL**. It falsely recorded owner acceptance and reviewer independence; corrected by `9364869`. Original FAIL remains (`milestones/016`). |
| `9364869` | Astra independent historical audit (Class B): **PASS**. Correctly retracts the premature closeout and distinguishes self-review, independent review, and owner acceptance (`milestones/016`). |
| `9de2c75` | Codex FAIL, four findings; corrected by `0f56f1e` (`milestones/017`). |
| `0f56f1e` | Codex PASS; Matt accepted milestone 017. |
| `8ce2893` | Astra independent historical audit (Class B): **PASS**. Milestone-017 closeout matches the accepted correction history (`milestones/017`). |
| `ae06f61` | Codex FAIL; corrected by `b9dfa49` (`milestones/018`). |
| `b9dfa49` | Codex FAIL; corrected by `f5246c0`. |
| `f5246c0` | Codex FAIL; corrected by `43379e0`. |
| `43379e0` | Codex PASS; Matt accepted milestone 018. |
| `b2d7779` | Astra independent historical audit (Class B): **PASS**. Milestone-018 closeout preserves its failed correction rounds and accepted state (`milestones/018`). |
| `99db4de` | Codex FAIL, three P2s; corrected by `c9af6b6` (`milestones/019`). |
| `c9af6b6` | Codex FAIL, two P2s; corrected by `c335d7c`. |
| `c335d7c` | Codex FAIL, two P2s; corrected by `8b5e70f`. |
| `8b5e70f` | Codex PASS; Matt accepted milestone 019. |
| `223c670` | Astra independent historical audit (Class B): **PASS**. Milestone-019 closeout matches its accepted parity-assurance history (`milestones/019`). |
| `f468e19` | Codex FAIL, one P1; corrected by `436f6c7` (`milestones/020`). |
| `436f6c7` | Codex FAIL, one P1 and two P2s; corrected by `34cd117`. |
| `34cd117` | Later Codex FAIL, one P1 already corrected by `8e6878e`. |
| `c25129a`, `826b1e2` | Later Codex PASS on both documentation commits; earlier weaker acceptance disclosed accurately. |
| `8e6878e` | Codex PASS; Matt accepted it as milestone 020's independently confirmed state. |
| `9ea0c9d` | Astra independent historical audit (Class B): **PASS**. Proposal scope preserves settled boundaries and does not authorize implementation (`milestones/021`). |
| `e65f0cd` | Codex FAIL, three defects and two documentation requirements; corrected by `ab737e1` (`milestones/021`). |
| `ccc1c26` | Astra independent historical audit (Class A): **FAIL**. Street-talk tests stopped before the consumer, later fixed by `4a5bacc`/`b4ce907`, and queued parity omitted provenance. `dfefbc1` corrected the live provenance defect and later passed independent review. Original FAIL remains (`milestones/022`). |
| `867922d` | Astra independent historical audit (Class B): **FAIL**. Roadmap prose granted unsupported review-gate bypasses. The playtest claim was later corrected; `bdcaab1` corrected the surviving additive-work exception and later passed independent review. Original FAIL remains (`milestones/025`). |
| `6738200` | Codex FAIL, one P1; corrected by `ba83b12` ([milestone 023](milestones/023-the-roster-reads.md)). |
| `4da1e66` | Codex FAIL, two P2s; corrected by `2dec7ff`. |
| `15d7c92` | Codex FAIL, two P2s; corrected by `53694a2`. |
| `f993386` | Codex FAIL, three findings; corrected by the milestone-024 chain (`milestones/024`). |
| `1a7bcc6` | Astra independent historical audit (Class A): **FAIL**. Findings covered the roadmap bypass, a projected-claim collision that could crash the knowledge screen, and misleading partial-answer wording. Later commits corrected the wording; `bdcaab1` passed its review, while `8fdf2e5` failed and is corrected by `86eef5e`. Original FAIL remains (`milestones/025`). |
| `a74bda7` | Astra independent historical audit (Class B): **PASS**. Reaction-channel candidate preserves pending rulings and information boundaries. |
| `ab737e1` | Codex FAIL, two P1s and one P2; corrected by `b02b003`. |
| `95e60b5` | Astra independent historical audit (Class A): **PASS WITH NOTES**. Correctly distinguishes admission, silence, and denial without reading private state (`milestones/025`). |
| `e58dbcc` | Astra independent historical audit (Class A): **PASS WITH NOTES**. Implementation met its authorized scope. Non-blocking note: `WrongReadChance` prose describes a conditional share although the interval is over all draws; no tuning change prescribed (`milestones/026`). |
| `57e4759`, `f476939` | Astra independent historical audit (Class B): **FAIL**. Reviewer instructions assumed worktree isolation without establishing it; corrected by `14ce1f5`. Preserve as one grouped review. |
| `3a45a27`, `e4df2ff` | Codex reviewed together and returned two P1s; corrected by `c644b30` (`milestones/026`). |
| `b02b003` | Codex FAIL, one further P1; corrected by `9fed181`. |
| `9fed181` | Codex PASS; Matt accepted milestone 021. |
| `c644b30` | Codex FAIL, one P1; corrected by `ab235b1`. |
| `ab235b1` | Codex confirmed runtime fix and returned one P2 test gap; corrected by `abcffd5`. |
| `abcffd5` | Codex PASS; Matt accepted milestone 026. This does not establish `e58dbcc`'s exact-diff status. |
| `14ce1f5` | Astra independent historical audit (Class B): **FAIL**. Worktree-safety correction was sound, but review-history claims were stale, independent-review coverage was overstated, and the roadmap bypass survived. `68a6c32` fixed the milestone-021 count and milestone-026 status label; `d6af1a6` later restored `e58dbcc`'s unresolved exact-commit coverage; `bdcaab1` corrected the bypass and later passed independent review. Original FAIL remains. |
| `68a6c32` | Astra independent historical audit (Class B): **PASS**. Corrected milestone 021's count from two follow-on corrections to three and changed ROADMAP's stale milestone-026 “unreviewed” label to closed and accepted. It did not correct the separate overstatement of independent review coverage. |
| `4a5bacc` | Astra independent historical audit (Class A): **FAIL**. Its owner/investigator boundary assertions were vacuous at the chosen seed; corrected by `b4ce907`. Original FAIL remains (`milestones/022`). |
| `b4ce907` | Codex FAIL, three explanation/inventory findings; corrected by `7cbeb91`. |
| `7cbeb91` | Codex FAIL, one P1 false-diff claim; corrected by `4ed58e3`. |
| `4ed58e3` | Astra independent historical audit (Class B): **PASS**. Append-only correction accurately distinguishes the real source-comment diff from unchanged runtime method bodies. Matt had already accepted it and closed the milestone-022 RNG correction chain. |
| `ba83b12` | Astra independent historical audit (Class A): **PASS WITH NOTES**. Trust-clamp history guard and production-path tests inspected statically ([milestone 023](milestones/023-the-roster-reads.md)). |
| `2dec7ff` | Codex FAIL, one P2 false-assurance test; corrected by `beff9ba`. |
| `beff9ba` | Astra independent historical audit (Class A): **PASS WITH NOTES**. Capability narration tests distinguish internal IDs from displayed names and exercise both bars ([milestone 023](milestones/023-the-roster-reads.md)). |
| `53694a2` | Codex FAIL, two P2s; corrected by `7036f0d`. |
| `7036f0d` | Astra independent historical audit (Class A): **PASS WITH NOTES**. Capability checks isolate the intended roster surface. Archive method-body and elapsed-day wording remains non-blockingly inaccurate ([milestone 023](milestones/023-the-roster-reads.md)). |
| `9ac569b` | Codex FAIL, three findings; corrected by `00613ca`. |
| `00613ca` | Codex FAIL, three findings; corrected by `dd59a1b`. |
| `dd59a1b` | Codex FAIL, one P2 record finding; corrected by `ed12b5e`. |
| `ed12b5e` | Codex FAIL, one P1 premature-review claim; corrected by `4137303`. |
| `4137303` | Astra independent historical audit (Class B): **PASS**. Correctly retracts the premature `ed12b5e` review claim without manufacturing acceptance. |
| `a17c8b6` | Astra independent historical audit (Class B): **PASS WITH NOTES**. Proposal intake remains noncanonical; external conversation completeness was not independently verified. |
| `ed7d38a` | Codex FAIL, five findings; corrected by `344f1e0`. |
| `344f1e0`, `762210f` | Claude independently reviewed both Codex-authored commits together: PASS with one P2 documentation finding; Matt accepted and closed milestone 024 at `762210f`. Preserve as one grouped review. |
| `c7dd34b` | Matt explicitly exempted this bookkeeping-only closeout from another immediate round. Closed by owner exception, not an independent review. |
| `d6af1a6` | Astra independently reviewed the Codex-authored cleanup (Class B): **PASS**. Its normalized ledger restored `e58dbcc` to unresolved exact-commit status, correcting `14ce1f5`'s overstatement of independent-review coverage. The exact three-file change otherwise preserves coverage, review distinctions, authorization boundaries, and proportional-review rules. Matt accepted the audit findings on 2026-09-12. |
| `da43fbb` | Astra independently reviewed this Claude-authored correction (Class B): FAIL, two P2s; omitted two original findings and undercounted six additional numeric occurrences as five. Matt accepted; correction continued at `bad7ad4` (`milestones/010`). |
| `bad7ad4` | Astra independently reviewed this Claude-authored correction (Class B): FAIL, two P2s; misidentified the original P2-2 and P2-3. Matt accepted; corrected at `0482dfc` (`milestones/010`). |
| `0482dfc` | Astra independently reviewed this Claude-authored correction (Class B): PASS, no blocking findings. Matt accepted on 2026-09-12, closing the grouped `bec0370`/`22e73d1`/`925611a` correction chain. Prior FAIL outcomes remain intact (`milestones/010`). |
| `891368d` (exact `891368d8d2dcac59ef817e110c9317319a79e65b`) | Astra independent exact-commit review (Class B): **PASS**, no findings. The closeout accurately records Matt's accepted `0482dfc` PASS, preserves every earlier FAIL, keeps the milestone-010 archive append-only, and does not claim to review itself. `git diff --check` passed; simulation tests were not rerun for this documentation-only diff. Matt accepted on 2026-09-12. |
| `dfefbc1` | Astra independent exact-commit review (Class A): **PASS**. Queued observation provenance comparison correction. Matt accepted on 2026-09-12 (`milestones/022`). |
| `bdcaab1` | Astra independent exact-commit review (Class B): **PASS**. Roadmap review-bypass correction. Matt accepted on 2026-09-12 (`milestones/025`). |
| `8fdf2e5` | Astra independent exact-commit review (Class A): **FAIL**, one P2 accepted by Matt. Grouping retained source accounts but omitted each incident's own position and basis; corrected by `86eef5e`, which later passed independent review and was accepted (`milestones/025`). |
| `61fa056` | Astra independent exact-commit review (Class B): **FAIL**, one P2 accepted by Matt. Audit verdicts replaced existing owner-acceptance facts; `b7344a5` restored those facts, and they remain preserved. |
| `86eef5e` | Astra independent exact-commit review (Class A): **PASS**. Every grouped incident retains and renders its own position, basis, and accounts. Matt accepted on 2026-09-12 (`milestones/025`). |
| `b7344a5` | Astra independent exact-commit review (Class B): **FAIL**, one P2 accepted by Matt. It conflated `68a6c32`'s status/count fixes with `d6af1a6`'s correction of overstated independent-review coverage and inaccurately said the prior finding concerned omitted Codex authorship. Corrected by `8651ecd`. |
| `8651ecd` (exact `8651ecd1c3b34fc30adec98637cedbaf265f8d14`) | Astra independent exact-commit review (Class B): **PASS**, no findings. The correction accurately separates the `68a6c32` and `d6af1a6` histories, removes the inaccurate authorship account, preserves prior verdicts and owner rulings, repairs all four milestone-023 links, and keeps the archive append-only. `git diff --check` passed; simulation tests were not rerun for this documentation-only diff. Matt accepted on 2026-09-12 (`milestones/023`). |
| `608f05e` (exact `608f05ef0e67149ba64a6ec50585fd5df6a30506`) | Astra independent exact-commit review (Class B): **PASS**, no findings. Astra did not author the inspected commit. The six-document transition accurately recorded the accepted historical PASS outcomes, closed the historical gate through `8651ecd`, preserved every prior FAIL, authorized Milestone 027 with R1–R6, and deferred finished-operation history. Matt accepted the outcome on 2026-09-12 before implementation began. |
| `64881a1` (exact `64881a162581b366314ace043c0a866ef0ddb7c3`) | Fresh Claude Opus 5 independent exact-commit review (Class A): **FAIL**, one P2 accepted by Matt on 2026-09-12. Runtime implementation met all six authorized rulings with no blocking code defect, but its archive claimed the accepted `608f05e` review without folding that outcome into this ledger as rule 8 required. Corrected by `303eed0`. Claude authored none of the inspected commits, edited nothing, and recorded no acceptance (`milestones/027`). |
| `303eed0` (exact `303eed0f1504c76c59671821877be9a98ed82b5e`) | **Closed by owner ruling, not an independent review.** Matt closed milestone 027 on 2026-09-12 and explicitly exempted this documentation-only record correction from a further review round, the same owner exception recorded for `c7dd34b`. No independent exact-commit review of this diff exists and none is claimed. The correction folded `608f05e`'s accepted PASS and `64881a1`'s accepted FAIL into the active range and advanced the verification baseline; `64881a1`'s FAIL and every earlier FAIL remain the historical outcomes for their hashes (`milestones/027`). |

## Current verification baseline

Latest independently reproduced baseline: `64881a1`.
Milestone 028's newer implementation measurements are in
`milestones/028-delegation-creates-bandwidth.md`; they are implementer evidence, not a replacement
for this independent baseline or a review outcome for the commit containing them.
Hashes are regression evidence, not permanent design requirements; an authorized behaviour change may
move them if the new values and reasons are recorded in its milestone archive.

- Build: 0 warnings, 0 errors.
- Tests: 707 passed, 0 failed.
- Trace / chosen-action hashes: `baseline` `92F742E3CB85E54B` / `BC280412B238B49F`;
  `cautious-vincent` `957DAC26D3DCBEF5` / `37640788CD6BA71B`; `watchful-boss`
  `38D0C93FB5F6B0AF` / `BC280412B238B49F`; `disloyal-vincent` `455A684A29A5F717` /
  `90C660AFB38741BB`; `resentful-tommy` `ADC3F2DDF1A9D50C` / `BC280412B238B49F`;
  `capable-angelo` `842B0968FB0388E9` / `2D16B6CD6153037C`.
- `--compare --seed 42`: 6 configurations, 6 distinct traces, 4 distinct chosen-action sequences.
- Both required viewpoint runs exit 0.
- Claude independently reproduced the three archive mutation checks and added two more. A disposable
  cross-process probe outside the repository confirmed the exact terminal instant, both natural
  outcomes, the `int.MaxValue` clamp, and post-load refusal across a real process boundary.
- Review limitation: no Godot binary was available to Claude, so the compiled project was verified
  by the build but none of the Godot self-tests were independently run. Their passing results remain
  implementer-reported in `milestones/027-the-session-has-an-ending.md`.

Run commands from `AGENTS.md` §Verification. Re-measure after the last edit that could affect the
reported value; never copy a number from an earlier worktree state.

## Class A review checklist

Use the applicable parts; record what was not run.

### Feature and architecture

- Does the exact diff implement the authorized feature rather than a nearby simplification?
- Does the core rule live in the simulation rather than only in presentation, a fixture, or a test?
- Can the same pipeline resolve controlled and autonomous actors without a player-only rule?
- Are authority, capability, ownership, executor identity, and information possession kept distinct?
- Is new persistent state deterministic, fingerprinted where appropriate, and reconstructed by save/load?
- Does the change preserve the engine-independent simulation boundary?

### Information safety

- Does each actor act only on knowledge, belief, observation, testimony, or authorized organizational
  state they may actually access?
- Can truth leak through candidate generation, names, DTOs, formatting, fixtures, or helper lookups?
- Are truth, trace, observation, claim, belief/knowledge, rumor, and audience-relative evidence kept
  distinct?
- Does provenance survive every write/read path, including repetition, contradiction, recantation,
  revision, save/load, and presentation?
- Does silence, withholding, or an unanswered request accidentally become knowledge?

### Determinism and state machines

- Are RNG keys causally local and stable under unrelated insertion?
- Can mutable state escape through a read-only interface, global field, collection, or replay seam?
- Are equivalent inputs deterministic across process restart, not merely twice in one process?
- Walk first acquisition, repetition, independent corroboration, contradiction, recantation,
  affirm→deny→affirm, and acquisition time versus reconsideration time where cognition changes.

### Tests and evidence

- Does each test invoke the production rule it claims to pin instead of copying its predicate?
- Does every negative test have a positive control and vice versa where vacuity is plausible?
- Was each load-bearing fix mutation-checked where practical, with the intended test failing for the
  intended reason?
- Do replay comparators include new persistent state without introducing false instability?
- Were baseline, stress variants, viewpoint output, and UI/persistence checks run when the diff touches
  them?

## Class B and C review checklist

For Class B:

- Identify the sentence that can authorize, block, accept, reject, canonize, or retire work.
- Trace every status and verification claim to the exact commit, owner ruling, review, or measurement.
- Confirm a proposal was not promoted to canon and a roadmap item was not treated as authorization.
- Confirm `CURRENT_MILESTONE.md` contains current truth only and names explicit in/out scope.
- Confirm the coverage table neither skips an intervening commit nor claims to review its own commit.
- Run only the repository/history checks needed to substantiate the prose; disclose anything not
  independently reproduced.

For Class C:

- Confirm the diff is genuinely non-authoritative and non-behavioural; otherwise escalate its class.
- Check links, paths, comments, formatting, and append-only placement.
- Check that summaries do not invent verification, acceptance, or design authority.

For every class, inspect the entire exact diff and record a concise “where to look” note naming the
claims that would be expensive if wrong.

## Design review questions

For a proposed gameplay system, ask:

1. What wakes the actor?
2. What information do they actually possess?
3. What occurs to them, and why?
4. What is available, and what is ruled out?
5. How do traits, drives, relationships, commitments, and policy affect evaluation without directly
   firing an action?
6. What persistent consequence or trace does the outcome leave?
7. Who can observe it, under what conditions?
8. How can the player learn it without omniscient truth?
9. Which distinct states might the implementation collapse?
10. What honest behaviour might a proposed correctness filter accidentally remove?

## Recurring cross-milestone failure patterns

Detailed instances live in milestone archives. Carry these questions into every relevant review:

- **A fix narrows expression.** What honest state or transition can no longer be represented?
- **A fix collapses distinct states.** What two different things are now treated as one?
- **A value's distinction stops halfway.** Where else is it written, copied, persisted, compared, or
  rendered?
- **A label substitutes for derivation.** Is a diagnostic grouping based on what the value reads, or
  merely what it is called?
- **A test assures itself.** Does it call production code, assert a recorded causal link, isolate the
  relevant rendered section, and fail when the fix is removed?
- **A negative path is vacuous.** Did the setup actually reach the branch under test?
- **An append-only archive is rewritten.** Is this a correction appended at EOF rather than a cleaner
  rewrite of what history originally said?
- **A measurement predates the last edit.** Was the figure re-derived after the final relevant change?
- **A review is inferred from silence.** Absence of a report is not evidence of PASS, FAIL, or no
  review; use `STATUS NOT ESTABLISHED`.
- **A gate goes stale.** Search for action-driving language—active, authorized, accepted, blocked,
  closed, next—not only for “awaiting review.”
- **A later correction hides an earlier gap.** Review exact commits in order; a clean descendant is
  not a review of its parent.

Review question to carry: **Which exact commit did this review inspect, who authored it, and is that
the same commit the record is about to call accepted or verified?**

## Historical names

Milestone archives correctly refer to retired files `CANONICAL_CODE_REVIEW_CONTEXT.md` and
`CANONICAL_DESIGN_CONTEXT.md`. They are historical references, not missing live dependencies; their
unique durable content was divided among this ledger, `ROADMAP.md`, `DESIGN_DECISIONS.md`, and
`OPEN_CONCERNS.md`.
