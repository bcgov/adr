import { defineConfig } from 'orval';

export default defineConfig({
  public_bodies: {
    input: {
        target: './Source/Api/Specification/openapi.yaml'
    },
    output: {
      client: 'zod',
      mode: 'single',
      target: './Source/Api/Specification/zod',
    },
  }
});