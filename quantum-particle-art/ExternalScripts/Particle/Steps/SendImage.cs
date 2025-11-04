using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;
using MailKit.Net.Smtp;
using MimeKit;
using UnityEngine.Assertions;

namespace UnityEngine.ExternalScripts.Particle.Steps;

public class SendImage : ParticleStep
{
    [Export] private Form _form;
    private Saver _saver;
    private bool _canSend = false; //Is set to true on init, and consumed once on release until next init

    private Dictionary<string, string> secrets;
    string secretFileName = "secrets.json";

    enum Keys
    {
        Host = 0,
        User = 1,
        Password = 2,
        DefaultTo = 3
    }

    string[] _keys = { "Host", "User", "Password", "DefaultTo" };
    private string Secret(Keys key) => secrets[_keys[(int)key]];

    public SendImage(Saver saver, Form form)
    {
        _form = form;
        _form.Visible = false;
        _form.OnSubmit += s =>
        {
            _form.Visible = false;
            Send(s);
        };
        _saver = saver;
        string file;
        try
        {
            file = File.ReadAllText(secretFileName);
        }
        catch (Exception e)
        {
            Debug.LogError(
                $"Error while reading secret file {secretFileName} under {Directory.GetCurrentDirectory()}, make sure it's been created properly on this machine. Exception: {e}");
            return;
        }

        secrets = JsonSerializer.Deserialize<Dictionary<string, string>>(file);
        Assert.IsTrue(_keys.All(k => secrets.ContainsKey(k) && !string.IsNullOrEmpty(secrets[k])),
            () => $"Make sure your {secretFileName} file contains all required keys: {string.Join(", ", _keys)}");
        //Send(Secret(Keys.DefaultTo), File.OpenRead(@"D:/Downloads/Black2048_times_2_final_8.png"),"Test mail with attachment");
    }

    public override async Task Init(WorldInitializer initializer)
    {
        await base.Init(initializer);
        _canSend = true;
        _form.Visible = false;
    }

    public override Task HandleParticles(ParticleWorld entry, float delay)
    {
        return Task.CompletedTask;
    }

    public override void Release()
    {
        base.Release();
        _form.Visible = true;
    }

    public bool Send(string mail)
    {
        if (_canSend)
        {
            Assert.IsTrue(_saver.Saved != null && _saver.Saved.Exists, "_saver.Saved!=null && _saver.Saved.Exists");
            Debug.Log($"Sending image {_saver.Saved.Name} in {_saver.Saved.Directory}");
            Debug.Log($"Sending image {_saver.Saved.Name} in {_saver.Saved.Directory}");
            if (string.IsNullOrEmpty(mail))
            {
                mail = Secret(Keys.DefaultTo);
                Debug.Log($"Null or empty mail got from field, falling back to default mail {mail} specified in secrets");   
            }
            if (Send(mail, File.OpenRead(_saver.Saved.FullName), _saver.Name))
            {
                _canSend = false;
                return true;
            }
        }

        return false;
    }

    public bool Send(string to, FileStream attachement, string name)
    {
        var server = Secret(Keys.Host);
        var user = Secret(Keys.User);
        var password = Secret(Keys.Password);
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("ESGI at Rome", user));
        var toName = string.Join(' ', to.Split('@')[0].Split('.'));
        message.To.Add(new MailboxAddress(toName, to));
        message.Subject = "Test mail'?";

        var fullBody = new Multipart();
        fullBody.Add(new TextPart("plain")
        {
            Text = "Here is the image you wanted."
        });
        fullBody.Add(new MimePart("image", "png")
        {
            Content = new MimeContent(attachement),
            ContentDisposition = new ContentDisposition(ContentDisposition.Attachment),
            ContentTransferEncoding = ContentEncoding.Base64,
            FileName = name
        });
        message.Body = fullBody;
        try
        {
            using (var client = new SmtpClient())
            {
                client.CheckCertificateRevocation = false;
                client.Connect(server, 465, true);
                client.Authenticate(user, password);
                client.Send(message);
                client.Disconnect(true);
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to send mail, with exception :", e);
            return false;
        }

        Debug.Log("Sent mail successfully to", to, toName);
        return true;
    }
}