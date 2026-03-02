import { useRef } from 'react';
import { useFrame } from '@react-three/fiber';
import { Float, MeshDistortMaterial, Sphere, MeshWobbleMaterial } from '@react-three/drei';
import * as THREE from 'three';

export function FloatingCrystal() {
  const meshRef = useRef<THREE.Mesh>(null);
  const shellRef = useRef<THREE.Mesh>(null);

  useFrame((state) => {
    const t = state.clock.getElapsedTime();
    if (meshRef.current) {
      meshRef.current.rotation.y = t * 0.2;
      meshRef.current.position.y = Math.sin(t) * 0.2;
    }
    if (shellRef.current) {
      shellRef.current.rotation.y = -t * 0.1;
      shellRef.current.rotation.z = t * 0.05;
    }
  });

  return (
    <group>
      <Float speed={1.5} rotationIntensity={0.5} floatIntensity={1}>
        {/* Core Orb */}
        <mesh ref={meshRef}>
          <sphereGeometry args={[1.5, 64, 64]} />
          <MeshDistortMaterial
            color="#6366f1"
            speed={4}
            distort={0.4}
            radius={1}
            emissive="#4338ca"
            emissiveIntensity={0.5}
            roughness={0}
            metalness={1}
          />
        </mesh>
        
        {/* Wireframe Shell */}
        <mesh ref={shellRef}>
          <sphereGeometry args={[2.2, 48, 48]} />
          <meshStandardMaterial
            color="#818cf8"
            wireframe
            transparent
            opacity={0.2}
            blending={THREE.AdditiveBlending}
            emissive="#818cf8"
            emissiveIntensity={0.2}
          />
        </mesh>

        {/* Inner Glow */}
        <Sphere args={[1.2, 32, 32]}>
          <MeshWobbleMaterial
            color="#ffffff"
            speed={3}
            factor={0.4}
            transparent
            opacity={0.05}
          />
        </Sphere>
      </Float>
    </group>
  );
}
