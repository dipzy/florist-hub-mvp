import { usePageText } from '../lib/usePageText';
import { lazy, Suspense, useEffect, useMemo, useRef, useState, type FormEvent } from 'react';
import { Link, useSearchParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { TenantLink } from '../components/TenantLink';
import { formatMoney } from '../lib/locale';
import { api } from '../api/client';
import type { CapacityDay, Flower, LegalVersions, PublicStoreCheckout } from '../api/types';
import { LegalAcceptanceFields } from '../components/LegalAcceptanceFields';
import { buildLegalAcceptance } from '../lib/legalConsent';
import type { ArrangementConfiguratorHandle } from '../configurator/ArrangementConfigurator';
import { resolveBaseType, shapeAllowsVase, type ArrangementBaseId, type ArrangementShapeId } from '../configurator/layout';
import { DeliveryDateTimePicker } from '../components/DeliveryDateTimePicker';
import { needsDeliveryZone } from '../lib/deliveryScheduling';
import { activeDeliveryOptions, validateRequestedDeliveryAt } from '../lib/storeOrderRules';
import { buildArrangementSubmitHint, canSubmitStoreOrder } from '../lib/storeRouteAccess';
import { useAuth } from '../context/AuthContext';
import { clearCheckoutDraft, loadCheckoutDraft, saveCheckoutDraft, type CheckoutDraft } from '../lib/checkoutDraft';
import { suggestDeliveryZoneId } from '../lib/suggestDeliveryZone';
import type { BasketLine } from '../lib/basket';
import { FlowerPicker } from '../components/checkout/FlowerPicker';
import { ArrangementBasketPanel } from '../components/checkout/ArrangementBasketPanel';
import { CheckoutStepper } from '../components/checkout/CheckoutStepper';
import { InlineSelect } from '../components/ui/InlineSelect';

const ArrangementConfigurator = lazy(() =>
  import('../configurator/ArrangementConfigurator').then((module) => ({ default: module.ArrangementConfigurator })),
);

export function BuildArrangementPage() {
  const { t } = useTranslation();
  const { pt } = usePageText('pages/BuildArrangementPage');
  const auth = useAuth();
  const submitIdempotencyRef = useRef<string | null>(null);
  const draftReadyRef = useRef(false);
  const [flowers, setFlowers] = useState<Flower[]>([]);
  const [capacityDays, setCapacityDays] = useState<CapacityDay[]>([]);
  const [checkout, setCheckout] = useState<PublicStoreCheckout | null>(null);
  const [basket, setBasket] = useState<BasketLine[]>([]);
  const [params] = useSearchParams();
  const inspireId = params.get('inspire') ?? undefined;
  const [inspireTitle, setInspireTitle] = useState<string | null>(null);
  const [deliveryType, setDeliveryType] = useState<number | ''>('');
  const [shape, setShape] = useState<ArrangementShapeId>(0);
  const [baseType, setBaseType] = useState<ArrangementBaseId>(1);
  const [variationSeed, setVariationSeed] = useState(17483021);
  const configuratorRef = useRef<ArrangementConfiguratorHandle>(null);
  const [deliveryAddress, setDeliveryAddress] = useState('');
  const [deliveryZoneId, setDeliveryZoneId] = useState('');
  const [requestedAt, setRequestedAt] = useState<Date | null>(null);
  const [notes, setNotes] = useState('');
  const [couponCode, setCouponCode] = useState('');
  const [taxId, setTaxId] = useState('');
  const [message, setMessage] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);
  const [legalVersions, setLegalVersions] = useState<LegalVersions | null>(null);
  const [acceptLegal, setAcceptLegal] = useState(false);
  const [checkoutStep, setCheckoutStep] = useState<1 | 2 | 3>(1);
  const [postalCode, setPostalCode] = useState('');
  const [submittedTrackPath, setSubmittedTrackPath] = useState<string | null>(null);

  const montarReturn = `/montar${inspireId ? `?inspire=${inspireId}` : ''}`;
  const authLink = `/login?returnUrl=${encodeURIComponent(montarReturn)}`;
  const registerLink = `/registo?returnUrl=${encodeURIComponent(montarReturn)}`;

  function persistDraft() {
    const draft: CheckoutDraft = {
      basket,
      deliveryType,
      deliveryAddress,
      deliveryZoneId,
      requestedAtIso: requestedAt ? requestedAt.toISOString() : null,
      notes,
      couponCode,
      taxId,
      shape,
      baseType,
      variationSeed,
      inspireId,
      checkoutStep,
      postalCode,
    };
    saveCheckoutDraft(draft);
  }

  const deliveryOptions = useMemo(
    () => (checkout ? activeDeliveryOptions(checkout) : []),
    [checkout],
  );

  const selectedZone = checkout?.deliveryZones.find((z) => z.id === deliveryZoneId);
  const configuratorItems = useMemo(
    () => basket.map((item) => ({ name: item.name, quantity: item.quantity, color: item.color })),
    [basket],
  );
  const itemsTotal = basket.reduce((sum, i) => sum + i.quantity * i.unitPrice, 0);
  const zoneCount = checkout?.deliveryZones.length ?? 0;
  const needsZone = deliveryType !== '' && needsDeliveryZone(deliveryType, zoneCount);
  const deliveryFee = deliveryType !== '' && deliveryType !== 0 && selectedZone ? selectedZone.fee : 0;
  const estimatedTotal = itemsTotal + deliveryFee;

  const ordersBlocked = checkout && !checkout.ordersAcceptingEnabled;
  const canSubmitOrder = canSubmitStoreOrder(auth.isAuthenticated, auth.role);
  const submitHint = buildArrangementSubmitHint(auth.isAuthenticated, auth.role);
  const needsAddress = deliveryType === 1 || deliveryType === 2;
  const addressLabel =
    deliveryType === 2 ? 'Local / morada (ex.: cemitério, talhão)' : 'Morada de entrega';

  useEffect(() => {
    const start = new Date();
    start.setHours(0, 0, 0, 0);
    const end = new Date(start);
    end.setDate(end.getDate() + 60);
    const iso = (date: Date) =>
      `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')}`;
    Promise.all([api.flowers(), api.storeCheckout(), api.legalVersions(), api.publicCapacity(iso(start), iso(end))])
      .then(([f, c, legal, capacity]) => {
        setFlowers(f);
        setCheckout(c);
        setLegalVersions(legal);
        setCapacityDays(capacity);
        const opts = activeDeliveryOptions(c);
        if (opts.length > 0) setDeliveryType(opts[0].value);
      })
      .catch(console.error);
  }, []);

  useEffect(() => {
    const draft = loadCheckoutDraft();
    if (draft) {
    setBasket(draft.basket);
    setDeliveryType(draft.deliveryType);
    setDeliveryAddress(draft.deliveryAddress);
    setDeliveryZoneId(draft.deliveryZoneId);
    setRequestedAt(draft.requestedAtIso ? new Date(draft.requestedAtIso) : null);
    setNotes(draft.notes);
    setCouponCode(draft.couponCode);
    setTaxId(draft.taxId);
    setShape(draft.shape as ArrangementShapeId);
    const draftShape = draft.shape as ArrangementShapeId;
    const draftBase = draft.baseType as ArrangementBaseId;
    setBaseType(resolveBaseType(draftShape, shapeAllowsVase(draftShape) && draftBase === 0));
    setVariationSeed(draft.variationSeed);
    if (draft.checkoutStep) setCheckoutStep(draft.checkoutStep);
    if (draft.postalCode) setPostalCode(draft.postalCode);
    clearCheckoutDraft();
    }
    draftReadyRef.current = true;
  }, []);

  useEffect(() => {
    if (!draftReadyRef.current) return;
    persistDraft();
  }, [
    basket,
    deliveryType,
    deliveryAddress,
    deliveryZoneId,
    requestedAt,
    notes,
    couponCode,
    taxId,
    shape,
    baseType,
    variationSeed,
    inspireId,
    checkoutStep,
    postalCode,
  ]);

  useEffect(() => {
    if (!checkout || !postalCode.trim() || deliveryZoneId) return;
    const suggested = suggestDeliveryZoneId(checkout, postalCode);
    if (suggested) setDeliveryZoneId(suggested);
  }, [postalCode, checkout, deliveryZoneId]);

  useEffect(() => {
    if (!inspireId) return;
    api
      .getPortfolioItem(inspireId)
      .then((p) => {
        setInspireTitle(p.title);
        const flowerById = new Map<string, Flower>();
        const flowerByName = new Map(flowers.map((f) => [f.name.toLowerCase(), f]));
        flowers.forEach((f) => flowerById.set(f.id, f));

        const prefill: BasketLine[] = [];
        for (const item of p.items) {
          if (item.isMaterial) continue;
          const flower = item.flowerId ? flowerById.get(item.flowerId) : flowerByName.get(item.name.toLowerCase());
          prefill.push({
            flowerId: flower?.id ?? item.flowerId,
            name: flower?.name ?? item.name,
            quantity: item.quantity,
            unitPrice: flower?.unitPrice ?? 0,
            color: flower?.color,
          });
        }
        if (prefill.length > 0) setBasket(prefill);
        if (p.description) setNotes((prev) => (prev ? prev : `Inspirado em «${p.title}»: ${p.description}`));
      })
      .catch(console.error);
  }, [inspireId, flowers]);

  function validateDeliveryStep(): string | null {
    if (deliveryType === '') return 'Selecione um tipo de entrega.';
    if (needsZone && !deliveryZoneId) return 'Selecione uma zona de entrega.';
    if (needsAddress && !deliveryAddress.trim()) {
      return deliveryType === 2
        ? 'Indique o local (ex.: cemitério e talhão).'
        : 'Indique a morada de entrega.';
    }
    return null;
  }

  const canGoConfirm = basket.length > 0 && validateDeliveryStep() === null;

  function selectCheckoutStep(step: 1 | 2 | 3) {
    if (step === 2 && basket.length === 0) {
      setMessage(t('build.addFlowersHint'));
      return;
    }
    if (step === 3) {
      const err = validateDeliveryStep();
      if (err) {
        setMessage(err);
        setCheckoutStep(2);
        return;
      }
    }
    setCheckoutStep(step);
    setMessage(null);
  }

  async function submit(e: FormEvent) {
    e.preventDefault();
    setMessage(null);
    if (!canSubmitOrder) {
      persistDraft();
      setMessage(submitHint ?? 'Entra ou cria uma conta de cliente para submeter a encomenda.');
      return;
    }
    if (!checkout) {
      setMessage('Não foi possível carregar as definições da loja.');
      return;
    }
    if (!checkout.ordersAcceptingEnabled) {
      setMessage('Esta loja não está a aceitar encomendas de momento.');
      return;
    }
    if (deliveryType === '') {
      setMessage('Selecione um tipo de entrega.');
      return;
    }
    if (needsAddress && !deliveryAddress.trim()) {
      setMessage(
        deliveryType === 2 ? 'Indique o local (ex.: cemitério e talhão).' : 'Indique a morada de entrega.',
      );
      return;
    }
    if (needsZone && !deliveryZoneId) {
      setMessage('Selecione uma zona de entrega.');
      return;
    }
    if (requestedAt) {
      const err = validateRequestedDeliveryAt(requestedAt, checkout.openingHours, checkout.minimumOrderAdvanceMinutes);
      if (err) {
        setMessage(err);
        return;
      }
    }
    if (!legalVersions || !acceptLegal) {
      setMessage('Tem de aceitar os Termos e a Política de Privacidade antes de submeter.');
      return;
    }
    const legal = buildLegalAcceptance(legalVersions, acceptLegal);
    setSubmitting(true);
    try {
      let arrangementPreviewUrl: string | null = null;
      try {
        const blob = await configuratorRef.current?.capturePng();
        if (blob) {
          const file = new File([blob], 'arranjo-3d.png', { type: 'image/png' });
          arrangementPreviewUrl = (await api.uploadPortfolioPhoto(file)).url;
        }
      } catch {
        arrangementPreviewUrl = null;
      }

      if (!submitIdempotencyRef.current) submitIdempotencyRef.current = crypto.randomUUID();
      const order = await api.createOrder({
        baseType: resolveBaseType(shape, shapeAllowsVase(shape) && baseType === 0),
        arrangementShape: shape,
        arrangementVariationSeed: variationSeed,
        arrangementPreviewUrl,
        deliveryType,
        deliveryAddress: needsAddress ? deliveryAddress.trim() : null,
        deliveryZoneId: needsZone ? deliveryZoneId || null : null,
        requestedDeliveryAt: requestedAt ? requestedAt.toISOString() : null,
        customerNotes: notes.trim() || null,
        customerTaxId: taxId.trim() || null,
        inspiredByPortfolioArrangementId: inspireId ?? null,
        items: basket.map((i) => ({
          flowerId: i.flowerId ?? null,
          name: i.name,
          quantity: i.quantity,
          unitPrice: i.unitPrice,
        })),
        acceptTermsAndPrivacy: legal.acceptTermsAndPrivacy,
        termsVersion: legal.termsVersion,
        privacyVersion: legal.privacyVersion,
        couponCode: couponCode.trim() || null,
      }, submitIdempotencyRef.current);
      submitIdempotencyRef.current = null;
      clearCheckoutDraft();
      const trackPath = order.trackingAccessToken ? `/acompanhar/${order.trackingAccessToken}` : null;
      setSubmittedTrackPath(trackPath);
      setMessage(
        `Encomenda submetida. Referência #${order.id.slice(0, 8)} — aguarda confirmação da florista.`,
      );
      setBasket([]);
      setDeliveryZoneId('');
      setCheckoutStep(1);
    } catch (err) {
      setMessage(err instanceof Error ? err.message : 'Erro ao submeter');
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <section className="page build-page">
      <header className="page-header" style={{ marginBottom: 'var(--fh-space-5)' }}>
        <h1 style={{ margin: 0 }}>Montar arranjo</h1>
      </header>
      {inspireId && (
        <p className="muted">
          Inspiração: {inspireTitle ?? inspireId.slice(0, 8)} —{' '}
          <Link to="/portfolio">voltar ao portfolio</Link>
        </p>
      )}

      {ordersBlocked && (
        <p className="error-banner" role="alert">
          Esta loja não está a aceitar encomendas de momento. Tente mais tarde ou contacte a florista.
        </p>
      )}

      <CheckoutStepper
        active={checkoutStep}
        onSelect={selectCheckoutStep}
        canGoDelivery={basket.length > 0}
        canGoConfirm={canGoConfirm}
      />

      {checkoutStep === 1 && (
        <div className="build-compose">
          <div className="build-compose__stage">
            <Suspense fallback={<p className="muted">A preparar o ramo…</p>}>
              <ArrangementConfigurator
                ref={configuratorRef}
                items={configuratorItems}
                shape={shape}
                baseType={baseType}
                seed={variationSeed}
                onShapeChange={setShape}
                onBaseTypeChange={setBaseType}
                onSeedChange={setVariationSeed}
              />
            </Suspense>
          </div>
          <div className="build-compose__side">
            <div className="card flower-picker">
              <FlowerPicker flowers={flowers} basket={basket} disabled={!!ordersBlocked} onChange={setBasket} />
            </div>
            <ArrangementBasketPanel
              basket={basket}
              deliveryFee={deliveryFee}
              estimatedTotal={estimatedTotal}
              onChange={setBasket}
              disabled={!!ordersBlocked}
              showContinue
              onContinue={() => setCheckoutStep(2)}
            />
          </div>
        </div>
      )}

      {checkoutStep === 1 && (
        <div className="build-mobile-bar">
          <div>
            <span className="muted">{t('build.estimatedTotal')}</span>
            <strong>{formatMoney(estimatedTotal)}</strong>
          </div>
          <button type="button" className="fh-btn fh-btn--primary" disabled={basket.length === 0 || !!ordersBlocked} onClick={() => setCheckoutStep(2)}>
            {t('build.continueDelivery')}
          </button>
        </div>
      )}

      <div className="build-flow">
        <form className="card form build-summary" onSubmit={submit} hidden={checkoutStep === 1}>
          <h2>{checkoutStep === 2 ? t('build.steps.delivery') : t('build.steps.confirm')}</h2>
          <ArrangementBasketPanel
            basket={basket}
            deliveryFee={deliveryFee}
            estimatedTotal={estimatedTotal}
            onChange={setBasket}
            disabled={!!ordersBlocked || submitting}
          />
          <div hidden={checkoutStep !== 2}>
          <label>
            Entrega
            <InlineSelect
              value={deliveryType}
              onChange={(ev) => {
                const v = ev.target.value === '' ? '' : Number(ev.target.value);
                setDeliveryType(v);
                if (v === 0) setDeliveryZoneId('');
              }}
              disabled={!!ordersBlocked || deliveryOptions.length === 0}
            >
              {deliveryOptions.length === 0 && <option value="">Sem tipos de entrega disponíveis</option>}
              {deliveryOptions.map((opt) => (
                <option key={opt.value} value={opt.value}>
                  {opt.label}
                </option>
              ))}
            </InlineSelect>
          </label>
          {needsZone && (
            <label>
              Zona de entrega
              <InlineSelect
                value={deliveryZoneId}
                onChange={(ev) => setDeliveryZoneId(ev.target.value)}
                required
                disabled={!!ordersBlocked}
              >
                <option value="">— Selecionar —</option>
                {checkout?.deliveryZones.map((z) => (
                  <option key={z.id} value={z.id}>
                    {z.name} — {formatMoney(z.fee)}
                  </option>
                ))}
              </InlineSelect>
            </label>
          )}
          {needsAddress && (
            <>
              <label>
                Código postal
                <input
                  value={postalCode}
                  onChange={(ev) => setPostalCode(ev.target.value)}
                  placeholder={pt("ex.: 1000-001")}
                  disabled={!!ordersBlocked}
                />
              </label>
              <label>
                {addressLabel}
                <input
                  value={deliveryAddress}
                  onChange={(ev) => setDeliveryAddress(ev.target.value)}
                  required
                  disabled={!!ordersBlocked}
                />
              </label>
            </>
          )}
          {checkout && (
            <DeliveryDateTimePicker
              value={requestedAt}
              onChange={setRequestedAt}
              openingHours={checkout.openingHours}
              minimumAdvanceMinutes={checkout.minimumOrderAdvanceMinutes}
              disabled={!!ordersBlocked}
              capacityDays={capacityDays}
            />
          )}
          <button
            type="button"
            className="secondary checkout-continue"
            onClick={() => {
              const err = validateDeliveryStep();
              if (err) {
                setMessage(err);
                return;
              }
              setCheckoutStep(3);
            }}
          >
            {t('build.continueToConfirm')}
          </button>
          </div>
          <div hidden={checkoutStep !== 3}>
          <details className="form-fold">
            <summary>Opcional</summary>
            <div className="form-fold__body">
              <label>
                NIF na fatura
                <input value={taxId} inputMode="numeric" onChange={(ev) => setTaxId(ev.target.value)} disabled={!!ordersBlocked} />
              </label>
              <label>
                Notas para a florista
                <textarea value={notes} onChange={(ev) => setNotes(ev.target.value)} rows={2} disabled={!!ordersBlocked} />
              </label>
              <label>
                Cupão
                <input
                  value={couponCode}
                  onChange={(ev) => setCouponCode(ev.target.value)}
                  disabled={!!ordersBlocked}
                  placeholder={pt("Código")}
                />
              </label>
            </div>
          </details>
          <LegalAcceptanceFields versions={legalVersions} checked={acceptLegal} onCheckedChange={setAcceptLegal} />
          {!canSubmitOrder && submitHint && (
            <p className="muted" role="status">
              {submitHint}{' '}
              <Link to={authLink} onClick={persistDraft}>
                Entrar
              </Link>
              {' · '}
              <Link to={registerLink} onClick={persistDraft}>
                Criar conta de cliente
              </Link>
            </p>
          )}
          <div className="checkout-actions">
          <button
            type="submit"
            disabled={basket.length === 0 || submitting || !!ordersBlocked || !canSubmitOrder}
          >
            Submeter encomenda
          </button>
          {message && (
            <p className={submittedTrackPath ? 'build-success' : 'muted'} role="status">
              {message}
            </p>
          )}
          {submittedTrackPath && (
            <p>
              <TenantLink to={submittedTrackPath} className="fh-btn fh-btn--primary">
                {t('order.tracking.openLink', { defaultValue: 'Acompanhar encomenda' })}
              </TenantLink>
            </p>
          )}
          <button type="button" className="secondary" onClick={() => setCheckoutStep(2)}>
            Voltar à entrega
          </button>
          </div>
          </div>
        </form>
      </div>
    </section>
  );
}
