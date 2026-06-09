import React from "react";

interface ErrorBoundaryProps {
  children: React.ReactNode;
}

interface ErrorBoundaryState {
  error: Error | null;
}

export class ErrorBoundary extends React.Component<ErrorBoundaryProps, ErrorBoundaryState> {
  public constructor(props: ErrorBoundaryProps) {
    super(props);
    this.state = { error: null };
  }

  public static getDerivedStateFromError(error: Error): ErrorBoundaryState {
    return { error };
  }

  public componentDidCatch(error: Error) {
    console.error("Frontend render failure", error);
  }

  public render() {
    if (!this.state.error) {
      return this.props.children;
    }

    return (
      <main className="mx-auto flex min-h-screen w-full max-w-3xl items-center px-4 py-12 sm:px-6">
        <section className="w-full rounded-3xl border border-rose-400/30 bg-slatepanel/90 p-8 shadow-panel">
          <p className="text-sm uppercase tracking-[0.2em] text-rose-300">Frontend Error</p>
          <h1 className="mt-3 font-display text-3xl text-white">The dashboard failed to render.</h1>
          <p className="mt-4 text-sm leading-7 text-slate-300">
            A browser-side runtime error prevented the interface from loading. Check the browser
            console and container logs, then reload after the fix is deployed.
          </p>
          <pre className="mt-6 overflow-x-auto rounded-2xl border border-white/8 bg-midnight/80 p-4 text-xs text-rose-200">
            {this.state.error.message}
          </pre>
        </section>
      </main>
    );
  }
}
