Shader "Custom/MovingStarfieldURP"
{
    Properties
    {
        _BackgroundColor ("Background Color", Color) = (0.05, 0.05, 0.1, 1)
        _StarColor ("Star Color", Color) = (1, 1, 1, 1)
        _Density ("Star Density", Float) = 20.0
        _SpeedX ("Speed X", Float) = 0.05
        _SpeedY ("Speed Y", Float) = 0.02
    }
    SubShader
    {
        Tags 
        { 
            "RenderType" = "Opaque" 
            "Queue" = "Background" 
            "RenderPipeline" = "UniversalPipeline"
        }
        LOD 100

        Pass
        {
            Name "ForwardLit"
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            // Подключение библиотек URP вместо старых встроенных
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float2 uv           : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float2 uv           : TEXCOORD0;
            };

            // Переменные, обернутые в CBUFFER для совместимости с SRP Batcher
            CBUFFER_START(UnityPerMaterial)
                half4 _BackgroundColor;
                half4 _StarColor;
                float _Density;
                float _SpeedX;
                float _SpeedY;
            CBUFFER_END

            // Хэш-функция для генерации псевдослучайных точек разного размера
            float2 hash22(float2 p)
            {
                p = float2(dot(p, float2(127.1, 311.7)), dot(p, float2(269.5, 183.3)));
                return frac(sin(p) * 43758.5453123);
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                // Использование URP-функции трансформации координат в пространство клиппинга
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Смещение координат со временем (в URP используется _Time.y)
                float2 movement = float2(_SpeedX, _SpeedY) * _Time.y;
                float2 uv = (input.uv + movement) * _Density;

                float2 id = floor(uv);
                float2 gv = frac(uv) - 0.5;

                float col = 0.0;

                // Проверка соседних ячеек для корректного отображения краев звезд
                for (int y = -1; y <= 1; y++)
                {
                    for (int x = -1; x <= 1; x++)
                    {
                        float2 offset = float2(x, y);
                        float2 n = hash22(id + offset);
                        
                        float2 starPos = offset + n - 0.5; 
                        float d = length(gv - starPos);
                        
                        // Случайный размер звезды на основе хэша
                        float starSize = n.x * 0.08; 
                        
                        float star = smoothstep(starSize, starSize - 0.02, d);
                        
                        // Добавление мерцания звезд
                        star *= 0.6 + 0.4 * sin(_Time.y * 3.0 + n.y * 6.28);
                        
                        col += star;
                    }
                }

                return lerp(_BackgroundColor, _StarColor, col);
            }
            ENDHLSL
        }
    }
}