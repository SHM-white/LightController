#include <FastLED.h>

#define LED_PIN 10
#define LED_NUMS 60
enum LED_MODE { 
  SOLID,
  RAINBOW, 
  BLINK,
  CHASE,
  RANDOM
};
LED_MODE currentMode = LED_MODE::SOLID;
int currentBrightness = 100;
CRGB leds[LED_NUMS];
CRGB defaultColor = CRGB(255, 255, 130);
void setup()
{
  // put your setup code here, to run once:
  FastLED.addLeds<WS2812B, LED_PIN, GRB>(leds, LED_NUMS);
  for (size_t i = 0; i < LED_NUMS; i++)
  {
    leds[i] = CRGB(100, 100, 100);
  }
  FastLED.show();
  Serial.begin(9600);
}
String message{""};
void loop()
{
  if(Serial.available() > 0){
    message = Serial.readStringUntil('\n');
    Serial.println(message);
    message.toLowerCase();
    if (message.startsWith("mode"))
    {
      String mode = message.substring(5);
      int modeInt = mode.toInt();
      switch (modeInt)
      {
      case LED_MODE::RAINBOW:
        currentMode = LED_MODE::RAINBOW;
        //setup
        for (size_t i = 0; i < LED_NUMS; i++)
        {
          leds[i] = CHSV(i * 255 / LED_NUMS, 255, 255);
        }
        break;
      case LED_MODE::SOLID:
        currentMode = LED_MODE::SOLID;
        break;
      case LED_MODE::BLINK:
        currentMode = LED_MODE::BLINK;
        break;
      case LED_MODE::CHASE:
        currentMode = LED_MODE::CHASE;
        {
          bool lit = true;
          for (size_t i = 0; i < LED_NUMS; i++)
          {
            if (lit)
            {
              leds[i] = defaultColor;
            }
            else
            {
              leds[i] = CRGB::Black;
            }
            if(i % 10 == 0)
            {
              lit = !lit;
            }
          }
        }
        break;
      case LED_MODE::RANDOM:
        currentMode = LED_MODE::RANDOM;
        for (size_t i = 0; i < LED_NUMS; i++)
        {
          leds[i] = CRGB::Black;
        }
        for (size_t i = 0; i < LED_NUMS / 2; i++)
        {
          leds[random(0, LED_NUMS)] = CHSV(random(0, 255), 255, 255);
        }
        break;
      
      default:
        break;
      }
    }
    else if (message.startsWith("light"))
    {
      int brightness = message.substring(6).toInt();
      if(brightness >= 0 && brightness <= 255){
        currentBrightness = brightness;
      }
    }
    else if (message.startsWith("color"))//"color r,g,b"
    {
      String color = message.substring(6);
      int r = color.substring(0, color.indexOf(',')).toInt();
      color.remove(0, color.indexOf(',') + 1);
      int g = color.substring(0, color.indexOf(',')).toInt();
      color.remove(0, color.indexOf(',') + 1);
      int b = color.toInt();
      defaultColor = CRGB(r, g, b);
    }
  }
  switch (currentMode)
  {
  case LED_MODE::RAINBOW:
    rainbow();
    break;
  case LED_MODE::SOLID:
    solid();
    break;
  case LED_MODE::BLINK:
    blink();
    break;
  case LED_MODE::CHASE:
    chase();
    break;
  case LED_MODE::RANDOM:
    randomLED();
    break;
  default:
    break;
  }
  FastLED.setBrightness(currentBrightness);
  FastLED.show();
  delay(10);
}
void solid()
{
  for (size_t i = 0; i < LED_NUMS; i++)
  {
    leds[i] = defaultColor;
  }
}
void randomLED()
{
  leds[random(0, LED_NUMS)] = CRGB::Black;
  leds[random(0, LED_NUMS)] = CHSV(random(0, 255), 255, 255);
  delay(100);
}
void rainbow()
{
  for (size_t i = 0; i < LED_NUMS; i++)
  {
    CHSV hsv = rgb2hsv_approximate(leds[i]);
    leds[i] = CHSV((hsv.hue + 5) % 255, 255, 255);
    //Serial.println(rgb2hsv_approximate(leds[i]).hue);
  }
  delay(100);
}
void blink()
{
  static bool lit = true;
  CRGB tmp = defaultColor;
  for (size_t i = 0; i < 150; i++)
  {
    if(lit){
      for (size_t j = 0; j < LED_NUMS; j++)
      {
        leds[j] = tmp / 100 * i;
      }
      FastLED.show();
    }
    else{
      for (size_t j = 0; j < LED_NUMS; j++)
      {
        leds[j] = tmp / 100 * (150 - i);
      }
      FastLED.show();
    }

  }
  
  lit = !lit;
  delay(1000);
}
void chase()
{
  CRGB tmp = leds[LED_NUMS - 1];
  for (size_t i = LED_NUMS - 1; i > 0; i--)
  {
    leds[i] = leds[i - 1];
  }
  leds[0] = tmp;
  delay(50);
}