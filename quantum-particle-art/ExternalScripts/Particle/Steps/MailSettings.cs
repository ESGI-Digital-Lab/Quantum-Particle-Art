using Godot;

namespace UnityEngine.ExternalScripts.Particle.Steps;
[GlobalClass]
public partial class MailSettings : Resource
{
    [Export] public string senderName = "ESGI at Rome";
    [Export] public string subject = "Your generated image during XXVIII Generative Art Conference live sessions";

    [Export(PropertyHint.MultilineText)] public string body =
        "Hello," +
        "\n\n" +
        "Here is the image you generated during the live sessions." +
        "\n\n" +
        "Best regards," +
        "\n"
        + "ESGI";
}