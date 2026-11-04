using System.Text.Json;
using System.Xml.Serialization;
using BtmsGateway.Contract.CustomsDeclarations;

namespace BtmsGateway.Contract.Test;

public class ClearanceRequestTest
{
    [Fact]
    public void When_deserializing_xml_then_serializing_to_json_should_preserve_contract_values()
    {
        const string xml = """
            <ALVSClearanceRequest xmlns="http://submitimportdocumenthmrcfacade.types.esb.ws.cara.defra.com">
              <ServiceHeader>
                <SourceSystem>CHIEF</SourceSystem>
                <DestinationSystem>ALVS</DestinationSystem>
                <CorrelationId>193264645102865</CorrelationId>
                <ServiceCallTimestamp>2022-01-12T14:21:50.286</ServiceCallTimestamp>
              </ServiceHeader>
              <Header>
                <EntryReference>290-000151G-02/01/2022</EntryReference>
                <EntryVersionNumber>3</EntryVersionNumber>
                <PreviousVersionNumber>2</PreviousVersionNumber>
                <DeclarationUCR>1GB116445386000-KDA35932Y1BHX</DeclarationUCR>
                <DeclarationType>S</DeclarationType>
                <ArrivalDateTime>202201031224</ArrivalDateTime>
                <SubmitterTURN>123456789012</SubmitterTURN>
                <DeclarantId>GB123456789013</DeclarantId>
                <DeclarantName>REDACTED</DeclarantName>
                <DispatchCountryCode>CN</DispatchCountryCode>
                <GoodsLocationCode>GBSTN</GoodsLocationCode>
                <MasterUCR>GB/CNS1-SCT16S6LF00000</MasterUCR>
              </Header>
              <Item>
                <ItemNumber>1</ItemNumber>
                <CustomsProcedureCode>4000000</CustomsProcedureCode>
                <TaricCommodityCode>2309103100</TaricCommodityCode>
                <GoodsDescription>DOG CHEWS</GoodsDescription>
                <ConsigneeId>GB123456789012</ConsigneeId>
                <ConsigneeName>REDACTED</ConsigneeName>
                <ItemNetMass>518.400</ItemNetMass>
                <ItemSupplementaryUnits>1.1</ItemSupplementaryUnits>
                <ItemThirdQuantity>1.2</ItemThirdQuantity>
                <ItemOriginCountryCode>CN</ItemOriginCountryCode>
                <Document>
                  <DocumentCode>N853</DocumentCode>
                  <DocumentReference>GBCVD2021.1396386</DocumentReference>
                  <DocumentStatus>AE</DocumentStatus>
                  <DocumentControl>P</DocumentControl>
                </Document>
                <Check>
                  <CheckCode>H222</CheckCode>
                  <DepartmentCode>PHA</DepartmentCode>
                </Check>
              </Item>
              <Item>
                <ItemNumber>2</ItemNumber>
                <CustomsProcedureCode>4000000</CustomsProcedureCode>
                <TaricCommodityCode>2309103100</TaricCommodityCode>
                <GoodsDescription>DOG CHEWS</GoodsDescription>
                <ConsigneeId>GB123456789012</ConsigneeId>
                <ConsigneeName>REDACTED</ConsigneeName>
                <ItemNetMass>8985.600</ItemNetMass>
                <ItemOriginCountryCode>CN</ItemOriginCountryCode>
                <Document>
                  <DocumentCode>N853</DocumentCode>
                  <DocumentReference>GBCVD2021.1396399</DocumentReference>
                  <DocumentStatus>AE</DocumentStatus>
                  <DocumentControl>P</DocumentControl>
                </Document>
                <Check>
                  <CheckCode>H222</CheckCode>
                  <DepartmentCode>PHA</DepartmentCode>
                </Check>
              </Item>
            </ALVSClearanceRequest>
            """;

        var request = DeserializeXml(xml);
        var json = JsonSerializer.Serialize(request);

        Assert.Equal("CHIEF", request.ServiceHeader.SourceSystem);
        Assert.Equal("ALVS", request.ServiceHeader.DestinationSystem);
        Assert.Equal("193264645102865", request.ServiceHeader.CorrelationId);
        Assert.Equal(new DateTime(2022, 1, 12, 14, 21, 50, 286), request.ServiceHeader.ServiceCallTimestamp);

        Assert.Equal("290-000151G-02/01/2022", request.Header.EntryReference);
        Assert.Equal(3, request.Header.EntryVersionNumber);
        Assert.Equal(2, request.Header.PreviousVersionNumber);
        Assert.Equal("1GB116445386000-KDA35932Y1BHX", request.Header.DeclarationUcr);
        Assert.Null(request.Header.DeclarationPartNumber);
        Assert.Equal("S", request.Header.DeclarationType);
        Assert.Equal(new DateTime(2022, 1, 3, 12, 24, 0), request.Header.ArrivalDateTime);
        Assert.Equal("123456789012", request.Header.SubmitterTurn);
        Assert.Equal("GB123456789013", request.Header.DeclarantId);
        Assert.Equal("REDACTED", request.Header.DeclarantName);
        Assert.Equal("CN", request.Header.DispatchCountryCode);
        Assert.Equal("GBSTN", request.Header.GoodsLocationCode);
        Assert.Equal("GB/CNS1-SCT16S6LF00000", request.Header.MasterUcr);

        Assert.Equal(2, request.Items.Length);

        Assert.Equal(1, request.Items[0].ItemNumber);
        Assert.Equal("4000000", request.Items[0].CustomsProcedureCode);
        Assert.Equal("2309103100", request.Items[0].TaricCommodityCode);
        Assert.Equal("DOG CHEWS", request.Items[0].GoodsDescription);
        Assert.Equal("GB123456789012", request.Items[0].ConsigneeId);
        Assert.Equal("REDACTED", request.Items[0].ConsigneeName);
        Assert.Equal(518.400m, request.Items[0].ItemNetMass);
        Assert.Equal(1.1m, request.Items[0].ItemSupplementaryUnits);
        Assert.Equal(1.2m, request.Items[0].ItemThirdQuantity);
        Assert.Equal("CN", request.Items[0].ItemOriginCountryCode);
        Assert.Single(request.Items[0].Documents!);
        Assert.Equal("N853", request.Items[0].Documents![0].DocumentCode);
        Assert.Equal("GBCVD2021.1396386", request.Items![0].Documents![0].DocumentReference);
        Assert.Equal("AE", request.Items![0].Documents![0].DocumentStatus);
        Assert.Equal("P", request.Items![0].Documents![0].DocumentControl);
        Assert.Null(request.Items![0].Documents![0].DocumentQuantity);
        Assert.Single(request.Items[0].Checks!);
        Assert.Equal("H222", request.Items[0].Checks![0].CheckCode);
        Assert.Equal("PHA", request.Items[0].Checks![0].DepartmentCode);

        Assert.Equal(2, request.Items[1].ItemNumber);
        Assert.Equal("4000000", request.Items[1].CustomsProcedureCode);
        Assert.Equal("2309103100", request.Items[1].TaricCommodityCode);
        Assert.Equal("DOG CHEWS", request.Items[1].GoodsDescription);
        Assert.Equal("GB123456789012", request.Items[1].ConsigneeId);
        Assert.Equal("REDACTED", request.Items[1].ConsigneeName);
        Assert.Equal(8985.600m, request.Items[1].ItemNetMass);
        Assert.Null(request.Items[1].ItemSupplementaryUnits);
        Assert.Null(request.Items[1].ItemThirdQuantity);
        Assert.Equal("CN", request.Items[1].ItemOriginCountryCode);
        Assert.Single(request.Items[1].Documents!);
        Assert.Equal("N853", request.Items[1].Documents![0].DocumentCode);
        Assert.Equal("GBCVD2021.1396399", request.Items[1].Documents![0].DocumentReference);
        Assert.Equal("AE", request.Items[1].Documents![0].DocumentStatus);
        Assert.Equal("P", request.Items[1].Documents![0].DocumentControl);
        Assert.Null(request.Items[1].Documents![0].DocumentQuantity);
        Assert.Single(request.Items[1].Checks!);
        Assert.Equal("H222", request.Items[1].Checks![0].CheckCode);
        Assert.Equal("PHA", request.Items[1].Checks![0].DepartmentCode);

        using var jsonDocument = JsonDocument.Parse(json);
        var root = jsonDocument.RootElement;

        Assert.Equal("CHIEF", root.GetProperty("serviceHeader").GetProperty("sourceSystem").GetString());
        Assert.Equal("ALVS", root.GetProperty("serviceHeader").GetProperty("destinationSystem").GetString());
        Assert.Equal("193264645102865", root.GetProperty("serviceHeader").GetProperty("correlationId").GetString());
        Assert.Equal("290-000151G-02/01/2022", root.GetProperty("header").GetProperty("entryReference").GetString());
        Assert.Equal(3, root.GetProperty("header").GetProperty("entryVersionNumber").GetInt32());
        Assert.Equal(2, root.GetProperty("header").GetProperty("previousVersionNumber").GetInt32());
        Assert.Equal(
            "1GB116445386000-KDA35932Y1BHX",
            root.GetProperty("header").GetProperty("declarationUCR").GetString()
        );
        Assert.Equal("S", root.GetProperty("header").GetProperty("declarationType").GetString());
        Assert.Equal("123456789012", root.GetProperty("header").GetProperty("submitterTURN").GetString());
        Assert.Equal("GB123456789013", root.GetProperty("header").GetProperty("declarantId").GetString());
        Assert.Equal("REDACTED", root.GetProperty("header").GetProperty("declarantName").GetString());
        Assert.Equal("CN", root.GetProperty("header").GetProperty("dispatchCountryCode").GetString());
        Assert.Equal("GBSTN", root.GetProperty("header").GetProperty("goodsLocationCode").GetString());
        Assert.Equal("GB/CNS1-SCT16S6LF00000", root.GetProperty("header").GetProperty("masterUCR").GetString());
        Assert.Equal(2, root.GetProperty("items").GetArrayLength());
    }

    [Fact]
    public void When_deserializing_json_then_serializing_to_xml_should_write_compact_xml_datetime_values()
    {
        const string json = """
            {
              "serviceHeader": {
                "sourceSystem": "CHIEF",
                "destinationSystem": "ALVS",
                "serviceCallTimestamp": "2022-01-03T12:24:00"
              },
              "header": {
                "entryReference": "25GB1HG99NHUJO3999",
                "entryVersionNumber": 3,
                "previousVersionNumber": 2,
                "declarationUCR": "1GB116445386000-KDA35932Y1BHX",
                "declarationPartNumber": "P1",
                "declarationType": "S",
                "arrivalDateTime": "2022-01-03T12:24:00",
                "submitterTURN": "123456789012",
                "declarantId": "GB123456789013",
                "declarantName": "REDACTED",
                "dispatchCountryCode": "CN",
                "goodsLocationCode": "GBSTN",
                "masterUCR": "GB/CNS1-SCT16S6LF00000"
              },
              "items": [
                {
                  "itemNumber": 1,
                  "customsProcedureCode": "4000000",
                  "documents": [
                    {
                      "documentCode": "N853"
                    }
                  ],
                  "checks": [
                    {
                      "checkCode": "H222",
                      "departmentCode": "PHA"
                    }
                  ]
                }
              ]
            }
            """;

        var request = JsonSerializer.Deserialize<ClearanceRequest>(json);

        Assert.NotNull(request);

        var xml = SerializeXml(request);

        Assert.Contains("<ArrivalDateTime>202201031224</ArrivalDateTime>", xml);
        Assert.Contains("<ServiceCallTimestamp>202201031224</ServiceCallTimestamp>", xml);
        Assert.Contains("<EntryReference>25GB1HG99NHUJO3999</EntryReference>", xml);
        Assert.Contains("<DeclarationUCR>1GB116445386000-KDA35932Y1BHX</DeclarationUCR>", xml);
        Assert.Contains("<SubmitterTURN>123456789012</SubmitterTURN>", xml);
        Assert.Contains("<MasterUCR>GB/CNS1-SCT16S6LF00000</MasterUCR>", xml);
        Assert.Contains("<ItemNumber>1</ItemNumber>", xml);
    }

    private static ClearanceRequest DeserializeXml(string xml)
    {
        var serializer = new XmlSerializer(typeof(ClearanceRequest));
        using var reader = new StringReader(xml);
        return (ClearanceRequest)serializer.Deserialize(reader)!;
    }

    private static string SerializeXml(ClearanceRequest clearanceRequest)
    {
        var serializer = new XmlSerializer(typeof(ClearanceRequest));
        using var writer = new StringWriter();
        serializer.Serialize(writer, clearanceRequest);
        return writer.ToString();
    }
}
