// The command channel: a file <name>.cmd in inbox/ holding a form body (op=give&prefab=Wood&amount=50); the answer lands
// in outbox/<name>.out (written to .tmp first, then moved, so a reader never sees half a file). Polled every 0.5 s,
// at most 20 files per poll. No network listener, nothing reachable from outside the machine.
using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace HomeCheat.Services
{
    internal sealed class FileInboxChannel
    {
        const float PollSeconds = 0.5f;
        const int MaxPerPoll = 20;

        readonly string inboxDir, outboxDir;
        readonly Func<string, string> handler;
        float nextPoll;

        public FileInboxChannel(string root, Func<string, string> handle)
        {
            inboxDir = Path.Combine(root, "inbox");
            outboxDir = Path.Combine(root, "outbox");
            handler = handle;
            Directory.CreateDirectory(inboxDir);
            Directory.CreateDirectory(outboxDir);
        }

        public string Inbox { get { return inboxDir; } }

        public void Poll()
        {
            if (Time.unscaledTime < nextPoll) return;
            nextPoll = Time.unscaledTime + PollSeconds;
            string[] files;
            try { files = Directory.GetFiles(inboxDir, "*.cmd"); }
            catch { return; }
            int n = 0;
            foreach (string f in files)
            {
                if (n++ >= MaxPerPoll) break;
                string body, result;
                try { body = File.ReadAllText(f, Encoding.UTF8); File.Delete(f); }
                catch { continue; }
                try { result = handler(body); }
                catch (Exception e) { result = "ERROR: " + e.Message; HomeApi.LogError("inbox: " + e); }
                Answer(Path.GetFileNameWithoutExtension(f), result);
            }
        }

        void Answer(string name, string result)
        {
            try
            {
                string tmp = Path.Combine(outboxDir, name + ".tmp");
                File.WriteAllText(tmp, result ?? "", Encoding.UTF8);
                string dst = Path.Combine(outboxDir, name + ".out");
                if (File.Exists(dst)) File.Delete(dst);
                File.Move(tmp, dst);
            }
            catch (Exception e) { HomeApi.LogError("outbox " + name + ": " + e.Message); }
        }
    }
}
