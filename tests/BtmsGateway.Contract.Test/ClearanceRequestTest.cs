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
                <ServiceCallTimestamp>202201031224</ServiceCallTimestamp>
              </ServiceHeader>
              <Header>
                <EntryReference>25GB1HG99NHUJO3999</EntryReference>
                <EntryVersionNumber>3</EntryVersionNumber>
                <PreviousVersionNumber>2</PreviousVersionNumber>
                <DeclarationType>S</DeclarationType>
                <ArrivalDateTime>202201031224</ArrivalDateTime>
              </Header>
              <Item>
                <ItemNumber>1</ItemNumber>
                <CustomsProcedureCode>4000000</CustomsProcedureCode>
                <Document>
                  <DocumentCode>N853</DocumentCode>
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

        Assert.Equal(new DateTime(2022, 1, 3, 12, 24, 0), request.Header.ArrivalDateTime);
        Assert.Equal(new DateTime(2022, 1, 3, 12, 24, 0), request.ServiceHeader.ServiceCallTimestamp);

        using var jsonDocument = JsonDocument.Parse(json);
        var root = jsonDocument.RootElement;

        Assert.Equal("CHIEF", root.GetProperty("serviceHeader").GetProperty("sourceSystem").GetString());
        Assert.Equal("25GB1HG99NHUJO3999", root.GetProperty("header").GetProperty("entryReference").GetString());
        Assert.Equal(3, root.GetProperty("header").GetProperty("entryVersionNumber").GetInt32());
        Assert.Equal(2, root.GetProperty("header").GetProperty("previousVersionNumber").GetInt32());
        Assert.Equal("S", root.GetProperty("header").GetProperty("declarationType").GetString());
        Assert.Equal(JsonValueKind.Number, root.GetProperty("header").GetProperty("arrivalDateTime").ValueKind);
        Assert.Equal(
            JsonValueKind.Number,
            root.GetProperty("serviceHeader").GetProperty("serviceCallTimestamp").ValueKind
        );
        Assert.Equal(1, root.GetProperty("items").GetArrayLength());
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
                "declarationType": "S",
                "arrivalDateTime": "2022-01-03T12:24:00"
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

        var xml = SerializeXml(request!);

        Assert.Contains("<ArrivalDateTime>202201031224</ArrivalDateTime>", xml);
        Assert.Contains("<ServiceCallTimestamp>202201031224</ServiceCallTimestamp>", xml);
        Assert.Contains("<EntryReference>25GB1HG99NHUJO3999</EntryReference>", xml);
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
