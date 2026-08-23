using CrimeEmpire.Persistence;

if (args.Length < 2)
{
    Console.Error.WriteLine("usage: CrimeEmpire.Persistence.InterruptedWriteHarness <realSavePath> <startedEventName>");
    Environment.Exit(64);
    return;
}

string realPath = args[0];
string eventName = args[1];

using var started = new EventWaitHandle(false, EventResetMode.ManualReset, eventName);

// Fires once WriteDatabase has created the .tmp file, committed its schema (auto-commit, outside any
// explicit transaction), and opened the transaction the meta/commands inserts would run in. Signaling
// here and then blocking forever means the parent test process can kill this process at a
// deterministic point — after a real write has genuinely begun, before it could possibly finish or be
// swapped onto the real path — via explicit synchronization rather than a timing guess.
SaveStore.OnTransactionOpenedForTest = () =>
{
    started.Set();
    Thread.Sleep(Timeout.Infinite);
};

var data = new SaveData(
    SchemaVersion: SaveStore.CurrentSchemaVersion,
    BuildId: SimulationBuild.CurrentId,
    Seed: 42,
    Variant: "baseline",
    ControlledCharacterId: "vincent",
    ViewpointCharacterId: "vincent",
    Commands: Array.Empty<SessionCommand>());

// Never returns under normal operation — the hook above blocks forever, and the parent kills this
// process while it is blocked. Reached only if the hook failed to fire, which means this harness is
// not testing what it claims to; fail loudly rather than let a silent exit 0 look like a pass.
SaveStore.Write(realPath, data);
Console.Error.WriteLine("harness wrote a save without ever pausing — the synchronization hook did not fire");
Environment.Exit(1);
