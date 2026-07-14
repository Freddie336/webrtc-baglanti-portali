using SIPSorcery.Net;
using System.Text.RegularExpressions;

namespace WebRTCSignalingServer;

internal static class SdpInspector
{
    public static bool TryValidate(string? sdp, out string error)
    {
        if (string.IsNullOrWhiteSpace(sdp))
        {
            error = "SDP içeriği boş olamaz.";
            return false;
        }

        if (sdp.Length > 128_000)
        {
            error = "SDP içeriği izin verilen boyutu aşıyor.";
            return false;
        }

        if (!Regex.IsMatch(sdp, @"(?m)^v=0\r?$")
            || !Regex.IsMatch(sdp, @"(?m)^m=(audio|video|application)\s"))
        {
            error = "SDP, v=0 sürüm satırı ve en az bir medya bölümü içermelidir.";
            return false;
        }

        try
        {
            _ = SDP.ParseSDPDescription(sdp);
            error = string.Empty;
            return true;
        }
        catch (Exception exception)
        {
            error = $"Geçersiz SDP: {exception.Message}";
            return false;
        }
    }
}
