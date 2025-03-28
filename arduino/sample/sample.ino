#include "SerialTransfer.h"

// Create a SerialTransfer object
SerialTransfer myTransfer;

// Define a struct that matches the C# TestStruct
struct TestStruct {
  int id;
  float temperature;
  float humidity;
  byte status;
} testData;

// Buffer for text messages
char message[64];

void setup() {
  // Start Serial communication
  Serial.begin(115200);
  
  // Start the SerialTransfer communication
  myTransfer.begin(Serial);
  
  // Print startup message to the Arduino Serial Monitor
  Serial.println("Arduino SerialTransfer Example");
  Serial.println("Ready to communicate with C# application");
}

void loop() {
  // Check for new packet
  if(myTransfer.available()) {
    // Get the packet ID
    uint8_t packetId = myTransfer.currentPacketID();
    
    // Process based on packet ID
    switch(packetId) {
      case 1: // Text message packet
        // Get the length of the packet
        uint16_t messageLength = myTransfer.bytesRead;
        
        // Copy data to our message buffer (ensure null termination)
        memset(message, 0, sizeof(message));
        for(uint16_t i = 0; i < min(messageLength, sizeof(message)-1); i++) {
          message[i] = myTransfer.packet.rxBuff[i];
        }
        
        // Print received message
        Serial.print("Received text message (Packet ID 1): ");
        Serial.println(message);
        
        // Send a response
        char response[] = "Message received by Arduino!";
        uint16_t len = strlen(response);
        for(uint16_t i = 0; i < len; i++) {
          myTransfer.packet.txBuff[i] = response[i];
        }
        myTransfer.sendData(len, 1);
        
        break;
        
      case 2: // Binary data packet (struct)
        // Use the rxObj() function to extract the struct data
        myTransfer.rxObj(testData);
        
        // Print received data
        Serial.print("Received binary struct (Packet ID 2): ");
        Serial.print("ID=");
        Serial.print(testData.id);
        Serial.print(", Temp=");
        Serial.print(testData.temperature);
        Serial.print("°C, Humidity=");
        Serial.print(testData.humidity);
        Serial.print("%, Status=");
        Serial.println(testData.status);
        
        // Modify the struct and send it back
        testData.temperature += 1.0;
        testData.humidity += 5.0;
        testData.status = 2;
        
        // Send the modified struct back
        myTransfer.sendDatum(testData, sizeof(testData));
        
        break;
        
      default:
        Serial.print("Unknown packet ID: ");
        Serial.println(packetId);
        break;
    }
  }
  
  // Add a small delay to prevent overwhelming the serial port
  delay(10);
}