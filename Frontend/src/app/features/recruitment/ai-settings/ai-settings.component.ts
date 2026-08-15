import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

import { IconComponent } from '../../../shared/icon.component';
import { RecruitmentAiService } from '../../../core/api/recruitment-ai.service';
import {
  AI_CAPABILITY_LABEL,
  AiProvider,
  FeatureSettings,
  TestApiKeyResult,
} from '../../../core/api/recruitment-ai.models';
import { environment } from '../../../../environments/environment';

/**
 * Everything that differs between providers, in one table.
 *
 * Before this existed, the screen mixed one provider's truths into every
 * provider's UI: an "IN-HOUSE" tagline under OpenAI, a `qwen3.5:27b` model
 * against Anthropic, a rainmaker.pk endpoint for Google. Each was a separate
 * small lie. Keeping the per-provider facts together means adding a provider
 * is one entry here rather than six scattered conditionals.
 */
interface ProviderProfile {
  value: AiProvider;
  label: string;
  hint: string;
  /** Ours: the service owns token budget and temperature, so those lock. */
  inHouse: boolean;
  defaultEndpoint: string;
  defaultModel: string;
  /** What the endpoint field actually means for this provider. */
  endpointHelp: string;
  /** False when this backend has no implementation to test the key against. */
  testable: boolean;
  /** True when generation returns a structured draft rather than prose. */
  structuredDrafts: boolean;
}

const PROVIDER_PROFILES: ProviderProfile[] = [
  {
    value: 'openai',
    label: 'OpenAI',
    hint: 'Bring your own OpenAI key. Usage is billed by OpenAI.',
    inHouse: false,
    defaultEndpoint: 'https://api.openai.com/v1',
    defaultModel: 'gpt-4o-mini',
    endpointHelp: 'API base URL. Change only if you use a proxy or Azure OpenAI.',
    testable: true,
    structuredDrafts: false,
  },
  {
    value: 'anthropic',
    label: 'Anthropic',
    hint: 'Bring your own Anthropic key. Usage is billed by Anthropic.',
    inHouse: false,
    defaultEndpoint: 'https://api.anthropic.com/v1',
    defaultModel: 'claude-sonnet-4-5',
    endpointHelp: 'API base URL. Change only if you route through a proxy.',
    testable: true,
    structuredDrafts: false,
  },
  {
    value: 'google',
    label: 'Google AI',
    hint: 'Bring your own Google AI key. Usage is billed by Google.',
    inHouse: false,
    defaultEndpoint: 'https://generativelanguage.googleapis.com/v1',
    defaultModel: 'gemini-2.0-flash',
    endpointHelp: 'API base URL for the Generative Language API.',
    testable: true,
    structuredDrafts: false,
  },
  {
    value: 'custom',
    label: 'Custom API',
    hint: 'Any other OpenAI-compatible service — Groq, DeepSeek, or self-hosted.',
    inHouse: false,
    defaultEndpoint: '',
    defaultModel: '',
    endpointHelp: "Your service's OpenAI-compatible base URL, ending before /chat/completions.",
    // The backend has no handler for `custom` yet: TestApiKeyAsync falls
    // through to "Unsupported provider for testing". Saying so up front beats
    // letting the button return a confusing failure.
    testable: false,
    structuredDrafts: false,
  },
  {
    value: 'multinetai',
    label: 'MultinetAI',
    hint: "Multinet's own AI service. No candidate data leaves our infrastructure.",
    inHouse: true,
    defaultEndpoint: 'https://ai.rainmaker.pk/hrms/api/v1',
    defaultModel: 'qwen3.5:27b',
    endpointHelp: 'Base URL only — the backend appends the feature path.',
    testable: true,
    structuredDrafts: true,
  },
];

@Component({
  selector: 'rm-ai-settings',
  standalone: true,
  imports: [FormsModule, IconComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './ai-settings.component.html',
})
export class AiSettingsComponent {
  private readonly api = inject(RecruitmentAiService);

  protected readonly providers = PROVIDER_PROFILES;

  /* ── Form state ───────────────────────────────────────────────────────── */
  protected provider = signal<AiProvider>('multinetai');
  protected apiKey = signal('');
  protected apiEndpoint = signal('https://ai.rainmaker.pk/hrms/api/v1');
  protected model = signal('qwen3.5:27b');
  protected maxTokens = signal(1000);
  protected temperature = signal(0);
  protected autoShortlistThreshold = signal(80);
  protected showKey = signal(false);

  protected features = signal<FeatureSettings>({
    autoScreening: true,
    autoMatching: true,
    generateQuestions: true,
    emailNotifications: true,
    autoParse: true,
  });

  /** Declared here rather than inline in the template so the keys stay
   *  type-checked against FeatureSettings — a typo would otherwise silently
   *  toggle nothing. */
  protected readonly featureRows: {
    key: keyof FeatureSettings;
    title: string;
    desc: string;
  }[] = [
    {
      key: 'autoScreening',
      title: 'Auto Resume Screening',
      desc: 'Score uploaded resumes against the job description when they arrive.',
    },
    {
      key: 'autoMatching',
      title: 'Auto Candidate Matching',
      desc: 'Suggest existing candidates from the job bank for new openings.',
    },
    {
      key: 'generateQuestions',
      title: 'Generate Interview Questions',
      desc: 'Draft technical and behavioural questions for each interview.',
    },
    {
      key: 'emailNotifications',
      title: 'Email Notifications',
      desc: 'Draft candidate emails for HR to review before anything is sent.',
    },
    {
      key: 'autoParse',
      title: 'Auto Resume Parse',
      desc: 'Extract profile fields from uploaded documents automatically.',
    },
  ];

  /* ── Async state ──────────────────────────────────────────────────────── */
  protected loading = signal(true);
  protected saving = signal(false);
  /** Tracks the feature-toggle save independently of the API-key save. */
  protected savingFeatures = signal(false);
  protected testing = signal(false);
  protected saveMessage = signal<{ ok: boolean; text: string } | null>(null);
  /** Separate message for the feature-toggle section so the two save flows
   *  don't overwrite each other's feedback. */
  protected featureSaveMessage = signal<{ ok: boolean; text: string } | null>(null);
  protected testResult = signal<TestApiKeyResult | null>(null);
  protected hasStoredKey = signal(false);

  /**
   * The in-house service owns token budgeting and pins temperature to 0 for
   * deterministic extraction, so those inputs are shown disabled rather than
   * hidden — hiding them would leave an administrator wondering where they
   * went, and a JD legitimately needs ~1700 tokens against a portal default
   * of 1000.
   */
  protected readonly isInHouse = computed(() => this.provider() === 'multinetai');

  protected readonly selectedProvider = computed(
    () => this.providers.find((p) => p.value === this.provider())!,
  );

  constructor() {
    this.load();
  }

  private load(): void {
    this.api.getApiKeySettings().subscribe((settings) => {
      if (settings) {
        this.provider.set((settings.provider?.toLowerCase() as AiProvider) ?? 'multinetai');
        this.apiEndpoint.set(settings.apiEndpoint ?? this.apiEndpoint());
        this.model.set(settings.model ?? this.model());
        this.maxTokens.set(settings.maxTokens || 1000);
        this.temperature.set(settings.temperature ?? 0);
        this.autoShortlistThreshold.set(settings.autoShortlistThreshold || 80);
        if (settings.settings) {
          this.features.set(settings.settings);
        }
        this.hasStoredKey.set(!!settings.apiKey);
      }
      this.loading.set(false);
    });
  }

  /**
   * Switching provider re-points the endpoint and model at that provider's
   * defaults.
   *
   * Carrying the previous provider's values across is how the screen ended up
   * offering an `ai.rainmaker.pk` endpoint for OpenAI — configuration that
   * cannot work, presented as if it could. Values the user has actually edited
   * are preserved; only untouched defaults are swapped.
   */
  protected setProvider(value: AiProvider): void {
    const from = this.providers.find((p) => p.value === this.provider())!;
    const to = this.providers.find((p) => p.value === value)!;

    if (this.apiEndpoint() === from.defaultEndpoint || !this.apiEndpoint()) {
      this.apiEndpoint.set(to.defaultEndpoint);
    }
    if (this.model() === from.defaultModel || !this.model()) {
      this.model.set(to.defaultModel);
    }

    this.provider.set(value);

    // The verdict on screen belongs to the provider it was run against.
    this.testResult.set(null);
  }

  protected toggleFeature(key: keyof FeatureSettings): void {
    this.features.update((f) => ({ ...f, [key]: !f[key] }));
    // Clear any stale feedback whenever the user touches a toggle.
    this.featureSaveMessage.set(null);
  }

  /**
   * Saves ONLY the five feature toggles and the auto-shortlist threshold.
   *
   * Uses the dedicated POST /SaveSettings endpoint, which never requires the
   * API key to be present in the request body — so the user never has to
   * re-enter it just to flip a toggle. The backend will return a clear error
   * if no API key row exists yet (configure the provider first).
   */
  protected saveFeatures(): void {
    this.savingFeatures.set(true);
    this.featureSaveMessage.set(null);

    this.api
      .saveFeatureSettings(
        environment.companyId,
        this.features(),
        this.autoShortlistThreshold(),
      )
      .subscribe((response) => {
        this.savingFeatures.set(false);
        this.featureSaveMessage.set({
          ok: response.isSuccess,
          text: response.isSuccess
            ? 'Feature settings saved.'
            : response.errors?.[0] || response.message,
        });
      });
  }

  protected save(): void {
    this.saving.set(true);
    this.saveMessage.set(null);

    this.api
      .saveApiKeySettings({
        companyId: environment.companyId,
        provider: this.provider(),
        apiKey: this.apiKey(),
        apiEndpoint: this.apiEndpoint(),
        model: this.model(),
        maxTokens: this.maxTokens(),
        temperature: this.temperature(),
        settings: this.features(),
      })
      .subscribe((response) => {
        this.saving.set(false);
        this.saveMessage.set({
          ok: response.isSuccess,
          text: response.isSuccess
            ? 'Settings saved.'
            : response.errors?.[0] || response.message,
        });

        if (response.isSuccess && this.apiKey()) {
          this.hasStoredKey.set(true);
          this.apiKey.set('');
        }
      });
  }

  protected confirmingDelete = signal(false);
  protected deleting = signal(false);

  /**
   * Soft-deletes the stored key. Two-step on purpose: it is the one action
   * here that silently disables every AI feature for the company, and an
   * accidental click would look like the AI service going down.
   */
  protected deleteKey(): void {
    if (!this.confirmingDelete()) {
      this.confirmingDelete.set(true);
      return;
    }

    this.deleting.set(true);
    this.api.deleteApiKey().subscribe((response) => {
      this.deleting.set(false);
      this.confirmingDelete.set(false);
      this.saveMessage.set({
        ok: response.isSuccess,
        text: response.isSuccess ? 'API key deleted.' : response.message,
      });

      if (response.isSuccess) {
        this.hasStoredKey.set(false);
        this.testResult.set(null);
      }
    });
  }

  protected test(): void {
    this.testing.set(true);
    this.testResult.set(null);

    this.api
      .testApiKey({
        companyId: environment.companyId,
        provider: this.provider(),
        // Blank means "test what is saved", which is what the button means
        // when the key field has not been touched.
        apiKey: this.apiKey(),
        apiEndpoint: this.apiEndpoint(),
      })
      .subscribe((result) => {
        this.testing.set(false);
        this.testResult.set(result);
      });
  }

  protected capabilityLabel(slug: string): string {
    return AI_CAPABILITY_LABEL[slug] ?? slug;
  }
}
