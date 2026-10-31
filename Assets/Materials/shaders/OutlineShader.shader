Shader "Custom/OutlineShader" // <-- Это путь, по которому вы найдете шейдер в Unity
{
    Properties
    {
        // Эти свойства будут отображаться в инспекторе материала
        _OutlineColor ("Outline Color", Color) = (1,1,0,1) // Цвет обводки (по умолчанию желтый)
        _OutlineWidth ("Outline Width", Range(0,0.1)) = 0.02 // Ширина обводки (от 0 до 0.1, по умолчанию 0.02)
    }
    SubShader
    {

        Tags { "RenderType"="Opaque" "Queue"="Transparent" } 
        LOD 100 // Уровень детализации

        // ----- ПЕРВЫЙ ПРОХОД: ОТРИСОВКА ОБВОДКИ -----
        Pass
        {
            ZWrite Off // Отключаем запись в Z-буфер, чтобы обводка не закрывала оригинальный объект
            Cull Front // Отсекаем передние грани. Это значит, что будут отрисованы только задние грани объекта.
                       // Когда мы слегка "раздуваем" объект, задние грани оказываются "снаружи" и формируют контур.
            Blend SrcAlpha OneMinusSrcAlpha // Включаем альфа-смешивание для прозрачности (если _OutlineColor имеет альфу)

            CGPROGRAM
            #pragma vertex vert // Определяем функцию-шейдер вершин
            #pragma fragment frag // Определяем функцию-шейдер фрагментов (пикселей)

            #include "UnityCG.cginc" // Включаем общие функции Unity

            // Структура данных, передаваемых из приложения в вершинный шейдер
            struct appdata
            {
                float4 vertex : POSITION; // Позиция вершины
                float3 normal : NORMAL;   // Нормаль вершины (направление "наружу")
            };

            // Структура данных, передаваемых из вершинного шейдера во фрагментный
            struct v2f
            {
                float4 vertex : SV_POSITION; // Координаты вершины на экране
                fixed4 color : COLOR;       // Цвет, который будет передан во фрагментный шейдер
            };

            // Переменные, которые будут получены из Properties шейдера (и из материала)
            fixed4 _OutlineColor;
            float _OutlineWidth;

            // Вершинный шейдер
            v2f vert (appdata v)
            {
                v2f o;

                v.vertex.xyz += v.normal * _OutlineWidth; 
                o.vertex = UnityObjectToClipPos(v.vertex); // Преобразуем позицию вершины в экранные координаты
                o.color = _OutlineColor; // Присваиваем цвет обводки
                return o;
            }

            // Фрагментный шейдер (пиксельный)
            fixed4 frag (v2f i) : SV_Target
            {
                return i.color; // Просто возвращаем цвет, полученный из вершинного шейдера
            }
            ENDCG
        }
    }
}