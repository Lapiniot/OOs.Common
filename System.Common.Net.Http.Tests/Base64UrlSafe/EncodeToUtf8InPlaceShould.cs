using static System.Net.Http.Base64UrlSafe;

namespace System.Common.Net.Http.Tests.Base64UrlSafe;

[TestClass]
public class EncodeToUtf8InPlaceShould
{
    [TestMethod]
    public void EncodesZeroBytes_GivenEmptyArray()
    {
        var bytes = Array.Empty<byte>();

        EncodeToUtf8InPlace(bytes, 0, out var bytesWritten);

        Assert.AreEqual(0, bytesWritten);
    }

    [TestMethod]
    public void EncodesZeroBytes_GivenInsufficientInputSize()
    {
        byte[] bytes = [131, 251, 62, 95];

        EncodeToUtf8InPlace(bytes, 6, out var bytesWritten);

        Assert.AreEqual(0, bytesWritten);
    }

    [TestMethod]
    public void EncodesZeroBytes_GivenInsufficientOutputBufferSize()
    {
        byte[] bytes = [131, 251, 62, 95];

        EncodeToUtf8InPlace(bytes, 4, out var bytesWritten);

        Assert.AreEqual(0, bytesWritten);
    }

    [TestMethod]
    public void Encodes6Bytes_WithUrlUnsafeReplaced_NoPadding_GivenSampleLength4_FitsScalarLoop()
    {
        byte[] bytes = [131, 251, 62, 95, 0, 0, 0, 0];

        EncodeToUtf8InPlace(bytes, 4, out var bytesWritten);

        Assert.AreEqual(6, bytesWritten);
        Assert.IsTrue(bytes.AsSpan(0, bytesWritten).SequenceEqual("g_s-Xw"u8));
    }

    [TestMethod]
    public void Encodes11Bytes_WithUrlUnsafeReplaced_NoPadding_GivenSampleLength8_FitsScalarLoop()
    {
        byte[] bytes = [235, 228, 32, 13, 35, 118, 111, 249, 0, 0, 0, 0, 0, 0, 0, 0];

        EncodeToUtf8InPlace(bytes, 8, out var bytesWritten);

        Assert.AreEqual(11, bytesWritten);
        Assert.IsTrue(bytes.AsSpan(0, bytesWritten).SequenceEqual("6-QgDSN2b_k"u8));
    }

    [TestMethod]
    public void Encodes16Bytes_WithUrlUnsafeReplaced_NoPadding_GivenSampleLength12_FitsVector128Exactly()
    {
        byte[] bytes = [79, 251, 215, 8, 91, 60, 249, 50, 17, 207, 57, 180, 0, 0, 0, 0, 0, 0];

        EncodeToUtf8InPlace(bytes, 12, out var bytesWritten);

        Assert.AreEqual(16, bytesWritten);
        Assert.IsTrue(bytes.AsSpan(0, bytesWritten).SequenceEqual("T_vXCFs8-TIRzzm0"u8));
    }

    [TestMethod]
    public void Encodes24Bytes_WithUrlUnsafeReplaced_NoPadding_GivenSampleLength18_FitsVector128WithReminder()
    {
        byte[] bytes = [82, 242, 177, 93, 98, 191, 128, 68, 1, 250, 159, 102, 139, 179, 231, 254, 141, 177, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0];

        EncodeToUtf8InPlace(bytes, 18, out var bytesWritten);

        Assert.AreEqual(24, bytesWritten);
        Assert.IsTrue(bytes.AsSpan(0, bytesWritten).SequenceEqual("UvKxXWK_gEQB-p9mi7Pn_o2x"u8));
    }

    [TestMethod]
    public void Encodes32Bytes_WithUrlUnsafeReplaced_NoPadding_GivenSampleLength24_FitsVector256Exactly()
    {
        byte[] bytes = [168, 245, 126, 112, 146, 60, 243, 144, 190, 15, 210, 106, 243, 246, 113, 69, 145, 131, 218, 187, 14, 74, 30, 6, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0];

        EncodeToUtf8InPlace(bytes, 24, out var bytesWritten);

        Assert.AreEqual(32, bytesWritten);
        Assert.IsTrue(bytes.AsSpan(0, bytesWritten).SequenceEqual("qPV-cJI885C-D9Jq8_ZxRZGD2rsOSh4G"u8));
    }

    [TestMethod]
    public void Encodes51Bytes_WithUrlUnsafeReplaced_NoPadding_GivenSampleLength38_FitsVector256WithReminder()
    {
        byte[] bytes = [102, 209, 44, 28, 33, 171, 17, 217, 224, 15, 39, 135, 254, 51, 130, 164, 213, 55, 253, 50, 37, 75, 229, 238, 125, 108, 202, 175, 116, 245, 151, 234, 128, 183, 251, 142, 114, 195, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0];

        EncodeToUtf8InPlace(bytes, 38, out var bytesWritten);

        Assert.AreEqual(51, bytesWritten);
        Assert.IsTrue(bytes.AsSpan(0, bytesWritten).SequenceEqual("ZtEsHCGrEdngDyeH_jOCpNU3_TIlS-XufWzKr3T1l-qAt_uOcsM"u8));
    }
}