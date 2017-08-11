{
    "Fragment": "
        #version 130

        uniform vec2 ViewSize;
        uniform vec3 CameraPosition;
        uniform float GameTime;
        
        uniform sampler2D blurHalfTex;
        uniform sampler2D blurQuarterTex;

        uniform sampler2D mainTex;
        uniform sampler2D lightTex;
        uniform sampler2D displacementTex;
        
        uniform float waterLevel;
        uniform float ambientLight;

        float wave(float x, float time) {
            float waveOffset = cos((x - time) * 60.0) * 0.004
                                + cos((x - 2.0 * time) * 20.0) * 0.008
                                + sin((x + 2.0 * time) * 35.0) * 0.01
                                + cos((x + 4.0 * time) * 70.0) * 0.001;
            return waveOffset * 0.4;
        }
        
        float aastep(float threshold, float value) {
            float afwidth = length(vec2(dFdx(value), dFdy(value))) * 0.70710678118654757;
            return smoothstep(threshold - afwidth, threshold + afwidth, value); 
        }

        void main() {
            vec3 waterColor = vec3(0.4, 0.6, 0.8);
            float time = GameTime * 0.065;
            vec2 viewSizeInv = (1.0 / ViewSize);

            vec2 uvWorld = gl_TexCoord[0].xy + (CameraPosition.xy * viewSizeInv.xy);

            float waveHeight = wave(uvWorld.x, time);
            float isTexelBelow = aastep(waveHeight, gl_TexCoord[0].y - waterLevel);
            float isTexelAbove = 1.0 - isTexelBelow;

            vec2 disPos = uvWorld.xy * vec2(0.4);
            disPos.x += mod(time, 2.0);
            disPos.y += mod(time, 2.0);
            vec2 dis = (texture2D(displacementTex, disPos).xy - vec2(0.5)) * vec2(0.014);
            
            vec2 uv = gl_TexCoord[0].xy + dis.xy * vec2(isTexelBelow);

            vec4 main = texture2D(mainTex, uv);
            float aberration = abs(gl_TexCoord[0].x - 0.5) * 0.012;
            float mainR = texture2D(mainTex, vec2(uv.x - aberration, uv.y)).r;
            float mainB = texture2D(mainTex, vec2(uv.x + aberration, uv.y)).b;
            
            float waterColBlendFac = isTexelBelow * 0.5;
            main.rgb = mix(main.rgb, waterColor * (0.4 + 1.2 * vec3(mainR, main.g, mainB)), vec3(waterColBlendFac));

            float topDist = abs(gl_TexCoord[0].y - waterLevel - waveHeight);
            float isNearTop = 1.0 - aastep(viewSizeInv.y * 2.8, topDist);
            float isVeryNearTop = 1.0 - aastep(viewSizeInv.y * 1.4, topDist);

            float topColorBlendFac = isNearTop * isTexelBelow;
            main.rgb = mix(main.rgb, texture2D(mainTex, vec2(gl_TexCoord[0].x,
                (waterLevel - gl_TexCoord[0].y + waterLevel) * 0.97 + waveHeight - viewSizeInv.y * 1.0
            )).rgb, vec3(topColorBlendFac));
            main.rgb += vec3(0.2 * isVeryNearTop);
            
            vec4 blur1 = texture2D(blurHalfTex, uv);
            vec4 blur2 = texture2D(blurQuarterTex, uv);
            vec4 light = texture2D(lightTex, uv);
            
            vec4 blur = (blur1 + blur2) * vec4(0.5);

            float gray = dot(blur.rgb, vec3(0.299, 0.587, 0.114));
            blur = vec4(gray, gray, gray, blur.a);

            gl_FragColor = mix(mix(
                                    main * (1.0 + light.g),
                                    blur,
                                    vec4(clamp((1.0 - light.r) / sqrt(max(ambientLight, 0.35)), 0.0, 1.0))
                                  ), vec4(0.0, 0.0, 0.0, 1.0), vec4(1.0 - light.r));
        }"
}