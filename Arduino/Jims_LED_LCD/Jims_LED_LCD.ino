// List of includes
#include <FastLED.h>
#include <Wire.h>
#include <LCD.h>
#include <LiquidCrystal_I2C.h>
#include <SPI.h>
#include <Adafruit_GFX.h>
#include <Adafruit_SSD1306.h>

#define VERSION 0.73

#define OLED_RESET 4
Adafruit_SSD1306 display(OLED_RESET);

#define PIN 6
#define LED_COUNT 150
struct CRGB leds[LED_COUNT];
LiquidCrystal_I2C lcd(0x3F, 2, 1, 0, 4, 5, 6 ,7);

long last_time;
bool Overflow = false;
int MaxFifo = 0; // Hold the largest Fifo Size

void setup()
{ 
  // Serial
  last_time = 0;
  Serial.begin(115200);

 // LCD Text
  lcd.begin (20,4,LCD_5x8DOTS);
  lcd.setBacklightPin(3, POSITIVE);
  lcd.setBacklight(HIGH);
  lcd.home();
  lcd.print("  Jim's LED Driver");
  lcd.setCursor(0,1);
  lcd.print("  Version:  ");
  lcd.print(VERSION);
  lcd.setCursor(0,0);

//  OLED Graphic
  display.begin(SSD1306_SWITCHCAPVCC, 0x3C);  // initialize with the I2C addr 0x3D (for the 128x64)
  display.setTextColor(WHITE,BLACK);
  display.clearDisplay();
  display.setCursor(0,0);
  display.setTextSize(2);
  display.println("Jim's");
  display.setCursor(65,0);
  display.setTextSize(1);
  display.print("LED Driver");
  display.setCursor(65,8);
  display.print("Ver: ");
  display.println(VERSION);
  
  display.setTextSize(3);
  display.setCursor(0,35);
  display.print("Mode:");
  display.display();

  // LEDS
  LEDS.addLeds<WS2812B, PIN, GRB>(leds, LED_COUNT);
  LEDS.showColor(CRGB(0, 0, 0));
  LEDS.setBrightness(70); // Limit max current draw to 1A
  LEDS.show(); 
}

// Send the Ready Signal
void SignalReady() {
  last_time = millis();
  Serial.write('R');
  Serial.write(MaxFifo);
}

// Read from the Serial port and display on LED strip
int pixelIndex;
int colorIndex;
uint8_t col[3]; 
bool LookForMode;
uint8_t Mode = 0;

void UpdateMode(uint8_t m) {
  if (m != Mode) {
    Mode = m;
    if (Mode == 100) {
//      lcd.clear();
//      lcd.print("GoodBye....");
/*      
      display.clearDisplay();
      display.setTextSize(2);
      display.setCursor(0,0);
      display.print("Goodbye...");
      display.display();
*/      
    } else {
//      lcd.setCursor(0,3);
//      lcd.print("Mode :    ");
//      lcd.setCursor(7, 3);
//      lcd.print(Mode);
//      delay(5);

//      display.setCursor(90,35);      
//      display.print(Mode);
//      display.display();
    }
  }
}

// Check Serial Port Fifo Size
void UpdateMaxFifo() {
  int fifocnt = Serial.available();
  // Check if the incomming fifo is full, if so we may have overflowed, can't find a real way 
  // to tell so this will have to do
  if (fifocnt == 63) {
    Overflow = true;
    Serial.write('S');
    MaxFifo = 0;
//    display.setCursor(90,35);      
//    display.print("Ovr");
//    display.display();
  } 
  
  if (fifocnt > MaxFifo) {
    MaxFifo = fifocnt;
//    display.setTextSize(1);
//    display.setCursor(0,16);      
//    display.print("Fifo: ");
//    display.print(MaxFifo);
//    display.display();
//    display.setTextSize(3);
    
    lcd.setCursor(0,2);
    lcd.print("Fifo : ");
    lcd.print(MaxFifo);
  }
}

void DoRead() {
  UpdateMaxFifo();
  uint8_t c = Serial.read();     
  if (LookForMode == true) {
    LookForMode = false; 
    UpdateMode(c);
  } else {
    if (c == 255) {
      colorIndex = 0;
      pixelIndex = 0;
      LookForMode = true;
      if (Overflow == false) {
        LEDS.show();
        delay(5);
        SignalReady(); 
      } else {
        Overflow = false;
      }
      Overflow = false;
    } else {
      col[colorIndex++] = c;
      if (colorIndex == 3) {
        leds[pixelIndex] = CRGB(col[0], col[1], col[2]);
        colorIndex = 0;
        pixelIndex++;
        if (pixelIndex >= LED_COUNT) {  // All values after read keep updating the last LED
          pixelIndex = LED_COUNT - 1;
        }
      } // index 3
    } // Color Pixel
  } // LookForMode
}

// Do Nothing, Everything in Events
void loop() {
  if ((millis() - last_time) > 1000) {
    SignalReady(); 
  }
  if (Serial.available()) {
    DoRead();
  }
}



