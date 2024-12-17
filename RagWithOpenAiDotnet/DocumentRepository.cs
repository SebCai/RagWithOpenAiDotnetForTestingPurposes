internal class DocumentRepository
{
    internal Stream GetTheDocument()
    {
        return BinaryData.FromBytes("""
    {
        "description": "This document contains the sale history data for Contoso products.",
        "sales": [
            {
                "month": "January",
                "by_product": {
                    "113043": 15,
                    "113045": 12,
                    "113049": 2
                }
            },
            {
                "month": "February",
                "by_product": {
                    "113045": 22
                }
            },
            {
                "month": "March",
                "by_product": {
                    "113045": 16,
                    "113055": 5
                }
            }
        ]
    }
    """u8.ToArray()).ToStream();
    }
}






