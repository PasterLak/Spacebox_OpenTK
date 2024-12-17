--Vert
#version 330 core
        layout (location = 0) in vec3 aPosition;
        layout (location = 1) in vec3 aColor;

        out vec3 vertexColor;

        uniform mat4 model;
        uniform mat4 view;
        uniform mat4 projection;

        void main()
        {
            gl_Position = projection * view * model * vec4(aPosition, 1.0);
            vertexColor = aColor;
        }

--Frag
        #version 330 core
        in vec3 vertexColor;
        out vec4 FragColor;

        void main()
        {
            FragColor = vec4(vertexColor, 1.0);
        }