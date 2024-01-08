namespace OOs.Common.Net.Http.Tests.Base64UrlSafe;

[TestClass]
public class ToBase64StringShould
{
    [TestMethod]
    [ExpectedException(typeof(ArgumentNullException))]
    public void ReturnEmptyString_GivenNullArray()
    {
        _ = OOs.Net.Http.Base64UrlSafe.ToBase64String(null);
    }

    [TestMethod]
    public void ReturnEmptyString_GivenEmptyArray()
    {
        var actual = OOs.Net.Http.Base64UrlSafe.ToBase64String([]);
        Assert.AreEqual("", actual);
    }

    [TestMethod]
    public void ReturnEncodedString_WithUrlUnsafeReplaced_NoPadding_GivenSampleLength4_FitsScalarLoop()
    {
        byte[] bytes = [131, 251, 62, 95];
        var actual = OOs.Net.Http.Base64UrlSafe.ToBase64String(bytes);
        Assert.AreEqual("g_s-Xw", actual);
    }

    [TestMethod]
    public void ReturnEncodedString_WithUrlUnsafeReplaced_NoPadding_GivenSampleLength8_FitsScalarLoop()
    {
        byte[] bytes = [235, 228, 32, 13, 35, 118, 111, 249];
        var actual = OOs.Net.Http.Base64UrlSafe.ToBase64String(bytes);
        Assert.AreEqual("6-QgDSN2b_k", actual);
    }

    [TestMethod]
    public void ReturnEncodedString_WithUrlUnsafeReplaced_NoPadding_GivenSampleLength12_FitsVector128Exactly()
    {
        byte[] bytes = [79, 251, 215, 8, 91, 60, 249, 50, 17, 207, 57, 180];
        var actual = OOs.Net.Http.Base64UrlSafe.ToBase64String(bytes);
        Assert.AreEqual("T_vXCFs8-TIRzzm0", actual);
    }

    [TestMethod]
    public void ReturnEncodedString_WithUrlUnsafeReplaced_NoPadding_GivenSampleLength18_FitsVector128WithReminder()
    {
        byte[] bytes = [82, 242, 177, 93, 98, 191, 128, 68, 1, 250, 159, 102, 139, 179, 231, 254, 141, 177];
        var actual = OOs.Net.Http.Base64UrlSafe.ToBase64String(bytes);
        Assert.AreEqual("UvKxXWK_gEQB-p9mi7Pn_o2x", actual);
    }

    [TestMethod]
    public void ReturnEncodedString_WithUrlUnsafeReplaced_NoPadding_GivenSampleLength24_FitsVector256Exactly()
    {
        byte[] bytes = [168, 245, 126, 112, 146, 60, 243, 144, 190, 15, 210, 106, 243, 246, 113, 69, 145, 131, 218, 187, 14, 74, 30, 6];
        var actual = OOs.Net.Http.Base64UrlSafe.ToBase64String(bytes);
        Assert.AreEqual("qPV-cJI885C-D9Jq8_ZxRZGD2rsOSh4G", actual);
    }

    [TestMethod]
    public void ReturnEncodedString_WithUrlUnsafeReplaced_NoPadding_GivenSampleLength38_FitsVector256WithReminder()
    {
        byte[] bytes = [102, 209, 44, 28, 33, 171, 17, 217, 224, 15, 39, 135, 254, 51, 130, 164, 213, 55, 253, 50, 37, 75, 229, 238, 125, 108, 202, 175, 116, 245, 151, 234, 128, 183, 251, 142, 114, 195];
        var actual = OOs.Net.Http.Base64UrlSafe.ToBase64String(bytes);
        Assert.AreEqual("ZtEsHCGrEdngDyeH_jOCpNU3_TIlS-XufWzKr3T1l-qAt_uOcsM", actual);
    }
}