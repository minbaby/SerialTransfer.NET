#include "SerialTransfer.h"

// Create a SerialTransfer object
SerialTransfer myTransfer;

// Define a struct that matches the C# TestStruct with proper packing
struct __attribute__((packed)) TestStruct
{
  int id;
  float temperature;
  float humidity;
  byte status;
} testData;

// Buffer for text messages
char message[64];
char response[64];

void setup()
{

  // Start Serial communication
  Serial.begin(115200);

  // Start the SerialTransfer communication
  myTransfer.begin(Serial);
}

void loop()
{
  // Check for new packet
  if (myTransfer.available())
  {
    // Get the packet ID
    uint8_t packetId = myTransfer.currentPacketID();

    if (packetId == 1)
    {
      // Get the length of the packet
      uint16_t messageLength = myTransfer.bytesRead;

      // Copy data to our message buffer (ensure null termination)
      memset(message, 0, sizeof(message));
      for (uint16_t i = 0; i < min(messageLength, sizeof(message) - 1); i++)
      {
        message[i] = myTransfer.packet.rxBuff[i];
      }

      // After acknowledgment, also send back a response message
      delay(100); // Small delay to separate the transmissions

      // Create a response message
      char textResponse[64];
      sprintf(textResponse, "Message '%s' processed by Arduino!", message);
      uint16_t textRespLen = strlen(textResponse);

      // Send the text response with packet ID 1
      for (uint16_t i = 0; i < textRespLen; i++)
      {
        myTransfer.packet.txBuff[i] = textResponse[i];
      }
      myTransfer.sendData(textRespLen, 1);
    }

    if (packetId == 2)
    {
      // Safety check to ensure enough data was received
      if (myTransfer.bytesRead >= sizeof(TestStruct))
      {

        // Copy the data manually byte by byte to avoid alignment issues
        uint8_t *src = myTransfer.packet.rxBuff;
        uint8_t *dest = (uint8_t *)&testData;

        for (uint16_t i = 0; i < sizeof(TestStruct); i++)
        {
          dest[i] = src[i];
        }

        // After acknowledgment, send a text response with modified ID
        delay(100); // Small delay to separate the transmissions

        // Create a more detailed response with ID+1
        char structResponse[64];
        sprintf(structResponse, "Processed struct with ID=%d", testData.id);
        uint16_t structRespLen = strlen(structResponse);

        // Send the text response with packet ID 1
        for (uint16_t i = 0; i < structRespLen; i++)
        {
          myTransfer.packet.txBuff[i] = structResponse[i];
        }
        myTransfer.sendData(structRespLen, 1);

        // Now send the struct data with increased ID
        delay(100);

        // Copy the struct to txBuff manually to ensure correct alignment
        testData.id += 1; // Increase the ID by 1
        testData.temperature += 1.0;
        testData.humidity += 5.0;
        testData.status = 2;

        memcpy(myTransfer.packet.txBuff, &testData, sizeof(TestStruct));
        myTransfer.sendData(sizeof(TestStruct), 2);
      }
    }
  }

  // Add a small delay to prevent overwhelming the serial port
  delay(10);
}