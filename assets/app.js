(function () {
  "use strict";

  const CONFIG = Object.freeze({
    currency: "INR",
    taxRate: 0.08,
    serviceCharge: 2,
    deliveryFee: 5,
    minCardDigits: 13,
    maxCartQtyPerItem: 20,
    storageKey: "bhai_g_cafe_cart_v1",
    menuPath: "assets/menu.json",
    processingDelayMs: 1600,
    apiBaseUrl: window.BHAI_G_CAFE_API_BASE || "http://localhost:8080"
  });

  const DEFAULT_MENU_ITEMS = Object.freeze([
    { id: "smoked-burrata", name: "Smoked Burrata & Heirloom Tomatoes", category: "starters", categoryLabel: "Starters", description: "Cold-smoked burrata on heirloom tomatoes, drizzled with aged balsamic and basil oil.", price: 14, unitLabel: "serving", image: "https://images.unsplash.com/photo-1603073163308-9654c3fb70b5?w=600&q=80", badge: { label: "Chef's Pick", tone: "new" } },
    { id: "crispy-polenta", name: "Crispy Polenta Bites", category: "starters", categoryLabel: "Starters", description: "Golden polenta cubes with whipped ricotta, sun-dried tomato tapenade, and fresh micro herbs.", price: 11, unitLabel: "serving", image: "https://images.unsplash.com/photo-1625944230945-1b7dd3b949ab?w=600&q=80", badge: { label: "Veg", tone: "veg" } },
    { id: "charred-prawn", name: "Charred Prawn Skewers", category: "starters", categoryLabel: "Starters", description: "Tiger prawns in chilli butter, flash-grilled over open flame, served with mango aioli.", price: 16, unitLabel: "serving", image: "https://images.unsplash.com/photo-1565299585323-38d6b0865b47?w=600&q=80", badge: { label: "Spicy", tone: "spicy" } },
    { id: "lobster-risotto", name: "Saffron Lobster Risotto", category: "mains", categoryLabel: "Mains", description: "Arborio rice in lobster bisque, topped with grilled lobster tail and saffron foam.", price: 38, unitLabel: "plate", image: "https://images.unsplash.com/photo-1476718406336-bb5a9690ee2a?w=600&q=80", badge: { label: "New", tone: "new" } },
    { id: "mushroom-wellington", name: "Wild Mushroom Wellington", category: "mains", categoryLabel: "Mains", description: "Forest mushroom duxelles in puff pastry with truffle jus and roasted root vegetables.", price: 26, unitLabel: "plate", image: "https://images.unsplash.com/photo-1547592180-85f173990554?w=600&q=80", badge: { label: "Veg", tone: "veg" } },
    { id: "short-rib", name: "Slow-Braised Short Rib", category: "mains", categoryLabel: "Mains", description: "48-hour braised beef short rib with smoky chipotle glaze, creamed mash, and pickled slaw.", price: 34, unitLabel: "plate", image: "https://images.unsplash.com/photo-1567620905732-2d1ec7ab7445?w=600&q=80", badge: { label: "Spicy", tone: "spicy" } },
    { id: "ribeye", name: "Ember-Kissed Ribeye", category: "grill", categoryLabel: "From the Grill", description: "28-day dry-aged 300g ribeye over wood ember, with bone marrow butter and house jus.", price: 52, unitLabel: "plate", image: "https://images.unsplash.com/photo-1544025162-d76594e3d3e0?w=600&q=80", badge: { label: "Signature", tone: "new" } },
    { id: "lemon-herb-chicken", name: "Spatchcock Lemon Herb Chicken", category: "grill", categoryLabel: "From the Grill", description: "Free-range chicken basted in preserved lemon and herb butter. Served with chimichurri.", price: 29, unitLabel: "plate", image: "https://images.unsplash.com/photo-1555939594-58d7cb561ad1?w=600&q=80", badge: null },
    { id: "cauliflower-steak", name: "Grilled Cauliflower Steak", category: "grill", categoryLabel: "From the Grill", description: "Whole cauliflower with harissa yogurt, pomegranate, toasted pine nuts, and fresh mint.", price: 22, unitLabel: "plate", image: "https://images.unsplash.com/photo-1558030006-450675393462?w=600&q=80", badge: { label: "Veg", tone: "veg" } },
    { id: "lava-cake", name: "Salted Caramel Lava Cake", category: "desserts", categoryLabel: "Desserts", description: "Dark chocolate fondant with molten salted caramel centre and vanilla bean gelato.", price: 13, unitLabel: "serving", image: "https://images.unsplash.com/photo-1551024601-bec78aea704b?w=600&q=80", badge: { label: "Must Try", tone: "new" } },
    { id: "yuzu-panna-cotta", name: "Yuzu Panna Cotta", category: "desserts", categoryLabel: "Desserts", description: "Silky yuzu panna cotta with blood orange compote and white chocolate shards.", price: 11, unitLabel: "serving", image: "https://images.unsplash.com/photo-1488477181946-6428a0291777?w=600&q=80", badge: { label: "Veg", tone: "veg" } },
    { id: "cheesecake", name: "Ash & Honey Cheesecake", category: "desserts", categoryLabel: "Desserts", description: "Baked cheesecake with activated charcoal crust, raw honey drizzle, and candied walnuts.", price: 12, unitLabel: "serving", image: "https://images.unsplash.com/photo-1571115177098-24ec42ed204d?w=600&q=80", badge: null },
    { id: "smoked-negroni", name: "Ember Smoked Negroni", category: "drinks", categoryLabel: "Drinks", description: "Classic Negroni finished with applewood smoke, served in a theatrical smoke-filled glass.", price: 16, unitLabel: "glass", image: "https://images.unsplash.com/photo-1551538827-9c037cb4f32a?w=600&q=80", badge: { label: "Signature", tone: "new" } },
    { id: "charcoal-lemonade", name: "Charcoal Lemonade", category: "drinks", categoryLabel: "Drinks", description: "Cold-pressed lemonade with activated charcoal, ginger, and a float of rose water.", price: 8, unitLabel: "glass", image: "https://images.unsplash.com/photo-1543253687-c931c8e01820?w=600&q=80", badge: { label: "Non-Alc", tone: "veg" } },
    { id: "turmeric-latte", name: "Gold Turmeric Latte", category: "drinks", categoryLabel: "Drinks", description: "Golden milk with turmeric, oat milk, cardamom, and black pepper. Warm or iced.", price: 7, unitLabel: "cup", image: "https://images.unsplash.com/photo-1560508180-03f285f67ded?w=600&q=80", badge: null }
  ]);

  const state = {
    cart: [],
    menu: [],
    activeFilter: "all",
    currentStep: 1,
    currentPayMethod: "card",
    selectedUpiApp: "",
    selectedWallet: "",
    publicConfig: {
      payment: {
        provider: "Razorpay",
        currency: "INR",
        onlineEnabled: false
      },
      notifications: {
        emailEnabled: false,
        smsEnabled: false
      }
    }
  };

  const elements = {
    body: document.body,
    menuGrid: document.getElementById("menuGrid"),
    menuState: document.getElementById("menuState"),
    pricingNote: document.getElementById("pricingNote"),
    filterButtons: Array.from(document.querySelectorAll("[data-filter]")),
    cartButton: document.getElementById("cartButton"),
    cartCount: document.getElementById("cartCount"),
    cartOverlay: document.getElementById("cartOverlay"),
    cartDrawer: document.getElementById("cartDrawer"),
    cartClose: document.getElementById("cartClose"),
    cartItems: document.getElementById("cartItems"),
    cartFooter: document.getElementById("cartFooter"),
    checkoutButton: document.getElementById("checkoutButton"),
    subtotalVal: document.getElementById("subtotalVal"),
    taxLabel: document.getElementById("taxLabel"),
    taxVal: document.getElementById("taxVal"),
    serviceLabel: document.getElementById("serviceLabel"),
    serviceVal: document.getElementById("serviceVal"),
    deliveryFeeRow: document.getElementById("deliveryFeeRow"),
    deliveryVal: document.getElementById("deliveryVal"),
    totalVal: document.getElementById("totalVal"),
    modalOverlay: document.getElementById("modalOverlay"),
    modalClose: document.getElementById("modalClose"),
    modalHeader: document.getElementById("modalHeader"),
    stepsBar: document.getElementById("stepsBar"),
    stepOneButton: document.getElementById("stepOneButton"),
    stepTwoButton: document.getElementById("stepTwoButton"),
    stepTwoBack: document.getElementById("stepTwoBack"),
    stepThreeBack: document.getElementById("stepThreeBack"),
    finalPayBtn: document.getElementById("finalPayBtn"),
    payBtnLabel: document.getElementById("payBtnLabel"),
    doneButton: document.getElementById("doneButton"),
    payMethodButtons: Array.from(document.querySelectorAll("[data-method]")),
    payMethodPanels: Array.from(document.querySelectorAll(".pay-method-panel")),
    upiApps: Array.from(document.querySelectorAll("[data-upi-app]")),
    walletApps: Array.from(document.querySelectorAll("[data-wallet]")),
    verifyUpiButton: document.getElementById("verifyUpiButton"),
    paymentConfigNote: document.getElementById("paymentConfigNote"),
    cardPanelHelp: document.getElementById("cardPanelHelp"),
    miniSummaryItems: document.getElementById("miniSummaryItems"),
    miniTotal: document.getElementById("miniTotal"),
    reviewItems: document.getElementById("reviewItems"),
    reviewSubtotal: document.getElementById("reviewSubtotal"),
    reviewTaxLabel: document.getElementById("reviewTaxLabel"),
    reviewTax: document.getElementById("reviewTax"),
    reviewServiceLabel: document.getElementById("reviewServiceLabel"),
    reviewService: document.getElementById("reviewService"),
    reviewDeliveryRow: document.getElementById("reviewDeliveryRow"),
    reviewDelivery: document.getElementById("reviewDelivery"),
    reviewTotal: document.getElementById("reviewTotal"),
    reviewDetails: document.getElementById("reviewDetails"),
    reviewPayment: document.getElementById("reviewPayment"),
    orderIdDisplay: document.getElementById("orderIdDisplay"),
    successNote: document.getElementById("successNote"),
    toast: document.getElementById("toast"),
    cardBrand: document.getElementById("cardBrand"),
    sections: {
      1: document.getElementById("section1"),
      2: document.getElementById("section2"),
      3: document.getElementById("section3"),
      processing: document.getElementById("sectionProcessing"),
      success: document.getElementById("sectionSuccess")
    },
    form: {
      fullName: document.getElementById("fullName"),
      email: document.getElementById("email"),
      phone: document.getElementById("phone"),
      deliveryType: document.getElementById("deliveryType"),
      addressGroup: document.getElementById("addressGroup"),
      address: document.getElementById("address"),
      tableGroup: document.getElementById("tableGroup"),
      tableNum: document.getElementById("tableNum"),
      specialInst: document.getElementById("specialInst"),
      cardName: document.getElementById("cardName"),
      cardNumber: document.getElementById("cardNumber"),
      cardExpiry: document.getElementById("cardExpiry"),
      cardCvv: document.getElementById("cardCvv"),
      upiId: document.getElementById("upiId")
    }
  };

  let toastTimer = 0;

  document.addEventListener("DOMContentLoaded", init);

  async function init() {
    hydrateCart();
    bindEvents();
    applyPricingLabels();
    renderCart();
    await loadPublicConfig();
    await loadMenu();
    updateDeliveryVisibility();
    selectPayMethod("card");
  }

  function bindEvents() {
    elements.filterButtons.forEach((button) => {
      button.addEventListener("click", () => setFilter(button.dataset.filter || "all"));
    });

    elements.cartButton.addEventListener("click", openCart);
    elements.cartClose.addEventListener("click", closeCart);
    elements.cartOverlay.addEventListener("click", closeCart);
    elements.checkoutButton.addEventListener("click", openPayment);

    document.querySelectorAll("[data-action='open-payment']").forEach((link) => {
      link.addEventListener("click", (event) => {
        event.preventDefault();
        openPayment();
      });
    });

    elements.modalClose.addEventListener("click", closePayment);
    elements.modalOverlay.addEventListener("click", (event) => {
      if (event.target === elements.modalOverlay) {
        closePayment();
      }
    });

    elements.stepOneButton.addEventListener("click", goToStep2);
    elements.stepTwoButton.addEventListener("click", goToStep3);
    elements.stepTwoBack.addEventListener("click", () => showStep(1));
    elements.stepThreeBack.addEventListener("click", () => showStep(2));
    elements.finalPayBtn.addEventListener("click", processPayment);
    elements.doneButton.addEventListener("click", completeOrder);

    elements.payMethodButtons.forEach((button) => {
      button.addEventListener("click", () => selectPayMethod(button.dataset.method || "card"));
    });

    elements.upiApps.forEach((button) => {
      button.addEventListener("click", () => selectChoice(elements.upiApps, button, "selectedUpiApp", button.dataset.upiApp || ""));
    });

    elements.walletApps.forEach((button) => {
      button.addEventListener("click", () => selectChoice(elements.walletApps, button, "selectedWallet", button.dataset.wallet || ""));
    });

    elements.verifyUpiButton.addEventListener("click", verifyUpi);
    elements.form.deliveryType.addEventListener("change", updateDeliveryVisibility);
    elements.form.cardNumber.addEventListener("input", formatCardNumber);
    elements.form.cardExpiry.addEventListener("input", formatCardExpiry);
    elements.form.cardCvv.addEventListener("input", digitsOnly);
    elements.form.phone.addEventListener("input", sanitizePhone);
    elements.menuGrid.addEventListener("click", handleMenuGridClick);
  }

  async function loadMenu() {
    try {
      const response = await fetch(CONFIG.menuPath, {
        headers: { "Accept": "application/json" },
        cache: "no-store"
      });
      if (!response.ok) {
        throw new Error("Menu request failed");
      }
      const data = await response.json();
      state.menu = Array.isArray(data) ? data.map(mapMenuItem).filter((item) => item && item.id) : [];
      if (state.menu.length === 0) {
        state.menu = DEFAULT_MENU_ITEMS.slice();
        elements.menuState.textContent = "Loaded fallback menu data.";
      }
      renderMenu();
    } catch (error) {
      console.error("Menu loading failed:", error);
      state.menu = DEFAULT_MENU_ITEMS.slice();
      renderMenu();
      elements.menuState.textContent = "Primary menu load failed, so fallback menu data is being shown.";
      showToast("Loaded fallback menu.");
    }
  }

  async function loadPublicConfig() {
    try {
      const response = await fetch(`${CONFIG.apiBaseUrl}/api/config/public`, {
        headers: { "Accept": "application/json" },
        cache: "no-store"
      });
      if (!response.ok) {
        throw new Error("Public config request failed");
      }

      const payload = await response.json();
      if (payload && typeof payload === "object") {
        state.publicConfig = {
          payment: {
            provider: sanitizeText(payload.payment && payload.payment.provider ? payload.payment.provider : "Razorpay"),
            currency: sanitizeText(payload.payment && payload.payment.currency ? payload.payment.currency : CONFIG.currency) || CONFIG.currency,
            onlineEnabled: Boolean(payload.payment && payload.payment.onlineEnabled)
          },
          notifications: {
            emailEnabled: Boolean(payload.notifications && payload.notifications.emailEnabled),
            smsEnabled: Boolean(payload.notifications && payload.notifications.smsEnabled)
          }
        };
      }
    } catch (error) {
      console.warn("Public config load failed:", error);
    }

    updatePaymentMethodAvailability();
  }

  function mapMenuItem(item, index) {
    const source = item && typeof item === "object" ? item : {};
    const name = sanitizeText(source.name || `Dish ${index + 1}`);
    const image = sanitizeText(source.image || "");
    const price = Number(source.price);

    return {
      id: sanitizeText(source.id || `dish-${index + 1}`),
      name,
      category: normalizeCategoryValue(source.category || "all"),
      categoryLabel: sanitizeText(source.categoryLabel || source.category || "Menu"),
      description: sanitizeText(source.description || "Chef-crafted dish."),
      price: Number.isFinite(price) ? price : 0,
      unitLabel: sanitizeText(source.unitLabel || "item"),
      image,
      badge: source.badge && typeof source.badge === "object" && source.badge.label
        ? {
            label: sanitizeText(source.badge.label),
            tone: sanitizeText(source.badge.tone || "")
          }
        : null
    };
  }

  function renderMenu() {
    const filteredItems = state.menu.filter((item) => matchesActiveFilter(item));
    const markup = filteredItems
      .map((item) => `<article class="card" data-menu-id="${escapeAttribute(item.id)}">${createMenuCardMarkup(item)}</article>`)
      .join("");
    elements.menuGrid.innerHTML = markup;

    if (filteredItems.length > 0 && elements.menuGrid.children.length === 0) {
      renderMenuFallback(filteredItems);
    }

    const showingCount = elements.menuGrid.children.length;
    window.__BHAI_G_MENU_DEBUG__ = {
      loaded: state.menu.length,
      activeFilter: state.activeFilter,
      filtered: filteredItems.length,
      rendered: showingCount
    };

    if (!showingCount) {
      elements.menuState.textContent = `No dishes found for this category. Loaded ${state.menu.length}, filtered ${filteredItems.length}, rendered ${showingCount}.`;
    } else {
      elements.menuState.textContent = `Loaded ${state.menu.length} dishes. Showing ${showingCount}.`;
    }
  }

  function renderMenuFallback(items) {
    elements.menuGrid.replaceChildren();
    const fragment = document.createDocumentFragment();
    items.forEach((item) => {
      const card = document.createElement("article");
      card.className = "card";
      card.dataset.menuId = item.id;
      card.innerHTML = createMenuCardMarkup(item);
      fragment.appendChild(card);
    });
    elements.menuGrid.appendChild(fragment);
  }

  function createMenuCardMarkup(item) {
    const badgeMarkup = item.badge && typeof item.badge.label === "string"
      ? `<span class="card-badge${item.badge.tone ? ` ${escapeAttribute(item.badge.tone)}` : ""}">${escapeHtml(item.badge.label)}</span>`
      : "";

    return `
      <div class="card-img" style="background-image:url('${escapeAttribute(encodeURI(item.image))}')">${badgeMarkup}</div>
      <div class="card-body">
        <div class="card-category">${escapeHtml(item.categoryLabel)}</div>
        <div class="card-title">${escapeHtml(item.name)}</div>
        <div class="card-desc">${escapeHtml(item.description)}</div>
        <div class="card-footer">
          <div class="price">${escapeHtml(formatMoney(item.price))}<span> / ${escapeHtml(item.unitLabel || "item")}</span></div>
          <button class="add-btn" type="button" aria-label="${escapeAttribute(`Add ${item.name} to cart`)}">+</button>
        </div>
      </div>
    `;
  }

  function setFilter(filter) {
    state.activeFilter = normalizeCategoryValue(filter);
    elements.filterButtons.forEach((button) => {
      button.classList.toggle("active", normalizeCategoryValue(button.dataset.filter || "all") === state.activeFilter);
    });
    renderMenu();
  }

  function matchesActiveFilter(item) {
    if (state.activeFilter === "all") {
      return true;
    }

    const category = normalizeCategoryValue(item.category);
    const categoryLabel = normalizeCategoryValue(item.categoryLabel);
    return category === state.activeFilter || categoryLabel === state.activeFilter;
  }

  function normalizeCategoryValue(value) {
    return String(value || "")
      .trim()
      .toLowerCase()
      .replace(/[^a-z0-9]+/g, "-")
      .replace(/^-+|-+$/g, "");
  }

  function handleMenuGridClick(event) {
    const button = event.target.closest(".add-btn");
    if (!button) {
      return;
    }

    const card = button.closest("[data-menu-id]");
    if (!card) {
      return;
    }

    const item = state.menu.find((menuItem) => menuItem.id === card.dataset.menuId);
    if (!item) {
      showToast("Could not find that menu item.");
      return;
    }

    addToCart(item, button);
  }

  function addToCart(item, button) {
    const existing = state.cart.find((entry) => entry.id === item.id);
    if (existing) {
      existing.qty = Math.min(existing.qty + 1, CONFIG.maxCartQtyPerItem);
    } else {
      state.cart.push({
        id: item.id,
        name: item.name,
        price: Number(item.price),
        image: item.image,
        qty: 1
      });
    }

    persistCart();
    renderCart();
    showToast(`"${item.name}" added to cart`);
    button.classList.add("added");
    button.textContent = "OK";
    window.setTimeout(() => {
      button.classList.remove("added");
      button.textContent = "+";
    }, 900);
  }

  function renderCart() {
    updateCartBadge();
    elements.cartItems.replaceChildren();

    if (state.cart.length === 0) {
      elements.cartFooter.hidden = true;
      elements.cartItems.appendChild(createCartEmptyState());
      updateTotals();
      return;
    }

    const fragment = document.createDocumentFragment();
    state.cart.forEach((item, index) => fragment.appendChild(createCartItem(item, index)));
    elements.cartItems.appendChild(fragment);
    elements.cartFooter.hidden = false;
    updateTotals();
  }

  function createCartEmptyState() {
    const wrapper = document.createElement("div");
    wrapper.className = "cart-empty";
    const text = document.createElement("p");
    text.textContent = "Your cart is empty. Add something delicious.";
    wrapper.appendChild(text);
    return wrapper;
  }

  function createCartItem(item, index) {
    const row = document.createElement("div");
    row.className = "cart-item";

    const image = document.createElement("div");
    image.className = "cart-item-img";
    image.style.backgroundImage = `url("${encodeURI(item.image)}")`;

    const details = document.createElement("div");
    const name = document.createElement("div");
    name.className = "cart-item-name";
    name.textContent = item.name;
    const price = document.createElement("div");
    price.className = "cart-item-price";
    price.textContent = `${formatMoney(item.price)} each`;
    details.append(name, price);

    const controls = document.createElement("div");
    controls.className = "cart-item-controls";
    controls.append(
      createQtyButton("-", () => changeQty(index, -1)),
      createQtyLabel(item.qty),
      createQtyButton("+", () => changeQty(index, 1))
    );

    row.append(image, details, controls);
    return row;
  }

  function createQtyButton(text, onClick) {
    const button = document.createElement("button");
    button.className = "qty-btn";
    button.type = "button";
    button.textContent = text;
    button.addEventListener("click", onClick);
    return button;
  }

  function createQtyLabel(quantity) {
    const label = document.createElement("span");
    label.className = "qty-num";
    label.textContent = String(quantity);
    return label;
  }

  function changeQty(index, delta) {
    const item = state.cart[index];
    if (!item) {
      return;
    }

    item.qty = Math.max(0, Math.min(CONFIG.maxCartQtyPerItem, item.qty + delta));
    if (item.qty === 0) {
      state.cart.splice(index, 1);
    }
    persistCart();
    renderCart();
  }

  function updateCartBadge() {
    const total = state.cart.reduce((sum, item) => sum + item.qty, 0);
    elements.cartCount.textContent = String(total);
    elements.cartCount.classList.remove("bump");
    void elements.cartCount.offsetWidth;
    elements.cartCount.classList.add("bump");
  }

  function hydrateCart() {
    try {
      const raw = window.localStorage.getItem(CONFIG.storageKey);
      if (!raw) {
        state.cart = [];
        return;
      }
      const parsed = JSON.parse(raw);
      state.cart = Array.isArray(parsed) ? parsed.filter(isValidCartItem).map((item) => ({
        id: item.id,
        name: item.name,
        price: Number(item.price),
        image: item.image,
        qty: Number(item.qty)
      })) : [];
    } catch (error) {
      state.cart = [];
      window.localStorage.removeItem(CONFIG.storageKey);
    }
  }

  function isValidCartItem(item) {
    return item &&
      typeof item.id === "string" &&
      typeof item.name === "string" &&
      typeof item.image === "string" &&
      Number.isFinite(Number(item.price)) &&
      Number.isInteger(Number(item.qty)) &&
      Number(item.qty) > 0 &&
      Number(item.qty) <= CONFIG.maxCartQtyPerItem;
  }

  function persistCart() {
    try {
      window.localStorage.setItem(CONFIG.storageKey, JSON.stringify(state.cart));
    } catch (error) {
      showToast("Could not persist cart on this device.");
    }
  }

  function getSubtotal() {
    return state.cart.reduce((sum, item) => sum + (item.price * item.qty), 0);
  }

  function getTax() {
    return getSubtotal() * CONFIG.taxRate;
  }

  function getDeliveryFee() {
    return elements.form.deliveryType.value === "delivery" ? CONFIG.deliveryFee : 0;
  }

  function getTotal() {
    return getSubtotal() + getTax() + CONFIG.serviceCharge + getDeliveryFee();
  }

  function updateTotals() {
    const subtotal = getSubtotal();
    const tax = getTax();
    const delivery = getDeliveryFee();
    const total = getTotal();

    elements.subtotalVal.textContent = formatMoney(subtotal);
    elements.taxVal.textContent = formatMoney(tax);
    elements.serviceVal.textContent = formatMoney(CONFIG.serviceCharge);
    elements.deliveryVal.textContent = formatMoney(delivery);
    elements.deliveryFeeRow.hidden = delivery === 0;
    elements.totalVal.textContent = formatMoney(total);
  }

  function applyPricingLabels() {
    const taxLabel = `Tax (${Math.round(CONFIG.taxRate * 100)}%)`;
    elements.taxLabel.textContent = taxLabel;
    elements.reviewTaxLabel.textContent = taxLabel;
    elements.serviceLabel.textContent = "Service charge";
    elements.reviewServiceLabel.textContent = "Service charge";
    elements.deliveryVal.textContent = formatMoney(CONFIG.deliveryFee);
    elements.reviewDelivery.textContent = formatMoney(CONFIG.deliveryFee);
    elements.pricingNote.textContent = `${taxLabel} + ${formatMoney(CONFIG.serviceCharge)} service charge${CONFIG.deliveryFee ? ` + ${formatMoney(CONFIG.deliveryFee)} delivery when applicable` : ""}.`;
  }

  function openCart() {
    elements.cartOverlay.hidden = false;
    elements.cartOverlay.classList.add("open");
    elements.cartDrawer.classList.add("open");
    elements.cartDrawer.setAttribute("aria-hidden", "false");
    elements.body.classList.add("no-scroll");
  }

  function closeCart() {
    elements.cartOverlay.classList.remove("open");
    elements.cartDrawer.classList.remove("open");
    elements.cartDrawer.setAttribute("aria-hidden", "true");
    elements.body.classList.remove("no-scroll");
    window.setTimeout(() => {
      if (!elements.cartOverlay.classList.contains("open")) {
        elements.cartOverlay.hidden = true;
      }
    }, 300);
  }

  function openPayment() {
    if (!state.cart.length) {
      showToast("Your cart is empty.");
      return;
    }
    closeCart();
    showStep(1);
    elements.modalOverlay.hidden = false;
    elements.modalOverlay.classList.add("open");
    elements.body.classList.add("no-scroll");
  }

  function closePayment() {
    elements.modalOverlay.classList.remove("open");
    elements.body.classList.remove("no-scroll");
    window.setTimeout(() => {
      if (!elements.modalOverlay.classList.contains("open")) {
        elements.modalOverlay.hidden = true;
      }
    }, 300);
  }

  function showStep(step) {
    state.currentStep = step;
    Object.values(elements.sections).forEach((section) => section.classList.remove("active"));
    if (elements.sections[step]) {
      elements.sections[step].classList.add("active");
    }

    [1, 2, 3].forEach((current) => {
      const marker = document.getElementById(`step${current}`);
      marker.classList.remove("active", "done");
      if (current < step) {
        marker.classList.add("done");
      } else if (current === step) {
        marker.classList.add("active");
      }
    });

    const showHeader = typeof step === "number" && step <= 3;
    elements.stepsBar.hidden = !showHeader;
    elements.modalHeader.hidden = !showHeader;
  }

  function goToStep2() {
    clearValidation();
    const fields = collectCustomerDetails();
    const error = validateCustomerDetails(fields);
    if (error) {
      markInvalid(error.field);
      showToast(error.message);
      return;
    }

    renderSummaryList(elements.miniSummaryItems, state.cart);
    elements.miniTotal.textContent = formatMoney(getTotal());
    showStep(2);
  }

  function goToStep3() {
    clearValidation();
    const error = validatePaymentDetails();
    if (error) {
      markInvalid(error.field);
      showToast(error.message);
      return;
    }

    renderSummaryList(elements.reviewItems, state.cart);
    elements.reviewSubtotal.textContent = formatMoney(getSubtotal());
    elements.reviewTax.textContent = formatMoney(getTax());
    elements.reviewService.textContent = formatMoney(CONFIG.serviceCharge);

    const deliveryFee = getDeliveryFee();
    elements.reviewDeliveryRow.hidden = deliveryFee === 0;
    elements.reviewDelivery.textContent = formatMoney(deliveryFee);
    elements.reviewTotal.textContent = formatMoney(getTotal());
    renderReviewDetails();
    renderReviewPayment();
    elements.payBtnLabel.textContent = state.currentPayMethod === "cod" ? "Place Order" : `Pay ${formatMoney(getTotal())}`;
    showStep(3);
  }

  async function processPayment() {
    try {
      if (!isOnlinePaymentEnabled() && state.currentPayMethod !== "cod") {
        throw new Error("Online payment is not available right now. Please use Cash or configure Razorpay on the server.");
      }

      showStep("processing");
      const response = await submitOrder();
      if (state.currentPayMethod === "cod") {
        finalizeSuccessfulOrder(response.orderId);
        return;
      }

      if (!response.paymentGateway || response.paymentGateway.provider !== "razorpay") {
        throw new Error("Online payment is not configured on the server yet.");
      }

      await openRazorpayCheckout(response);
    } catch (error) {
      showStep(3);
      showToast(error && error.message ? error.message : "Order could not be processed.");
    }
  }

  function completeOrder() {
    resetCheckoutForm();
    closePayment();
  }

  function selectPayMethod(method) {
    if (method !== "cod" && !isOnlinePaymentEnabled()) {
      showToast("Online payment is currently unavailable. Please use Cash.");
      method = "cod";
    }

    state.currentPayMethod = method;
    elements.payMethodButtons.forEach((button) => {
      button.classList.toggle("active", button.dataset.method === method);
    });
    elements.payMethodPanels.forEach((panel) => {
      panel.classList.toggle("active", panel.id === `panel-${method}`);
    });
  }

  function selectChoice(group, button, stateKey, value) {
    group.forEach((item) => item.classList.remove("selected"));
    button.classList.add("selected");
    state[stateKey] = value;
  }

  function verifyUpi() {
    const upiId = sanitizeText(elements.form.upiId.value);
    if (!/^[a-zA-Z0-9._-]{2,}@[a-zA-Z]{2,}$/u.test(upiId)) {
      markInvalid(elements.form.upiId);
      showToast("Enter a valid UPI ID, for example name@okaxis.");
      return;
    }
    clearInvalid(elements.form.upiId);
    showToast("UPI ID looks valid.");
  }

  function updateDeliveryVisibility() {
    const deliveryType = elements.form.deliveryType.value;
    elements.form.addressGroup.hidden = deliveryType !== "delivery";
    elements.form.tableGroup.hidden = deliveryType !== "dine";
    updateTotals();
  }

  function renderSummaryList(container, items) {
    container.replaceChildren();
    const fragment = document.createDocumentFragment();

    items.forEach((item) => {
      const row = document.createElement("div");
      row.className = "summary-item";

      const left = document.createElement("span");
      left.textContent = `${item.name} x ${item.qty}`;

      const right = document.createElement("span");
      right.textContent = formatMoney(item.price * item.qty);

      row.append(left, right);
      fragment.appendChild(row);
    });

    container.appendChild(fragment);
  }

  function renderReviewDetails() {
    const details = collectCustomerDetails();
    elements.reviewDetails.replaceChildren();

    const lines = [
      { label: "Name", value: details.fullName },
      { label: "Email", value: details.email },
      { label: "Phone", value: details.phone },
      { label: "Order Type", value: readableDeliveryType(details.deliveryType) }
    ];

    if (details.deliveryType === "delivery") {
      lines.push({ label: "Address", value: details.address });
    }
    if (details.deliveryType === "dine" && details.tableNum) {
      lines.push({ label: "Table", value: details.tableNum });
    }
    if (details.specialInst) {
      lines.push({ label: "Instructions", value: details.specialInst });
    }

    lines.forEach((line) => {
      const entry = document.createElement("p");
      const label = document.createElement("strong");
      label.textContent = `${line.label}: `;
      entry.appendChild(label);
      entry.appendChild(document.createTextNode(line.value));
      elements.reviewDetails.appendChild(entry);
    });
  }

  function renderReviewPayment() {
    let description = "";
    switch (state.currentPayMethod) {
      case "card":
        description = "Payment via Credit / Debit Card";
        break;
      case "upi":
        description = `Payment via UPI${state.selectedUpiApp ? ` (${state.selectedUpiApp})` : ""}`;
        break;
      case "wallet":
        description = `Payment via Digital Wallet${state.selectedWallet ? ` (${state.selectedWallet})` : ""}`;
        break;
      default:
        description = "Payment via Cash on Delivery";
    }
    elements.reviewPayment.textContent = description;
  }

  function collectCustomerDetails() {
    return {
      fullName: sanitizeText(elements.form.fullName.value),
      email: sanitizeText(elements.form.email.value).toLowerCase(),
      phone: sanitizePhoneValue(elements.form.phone.value),
      deliveryType: elements.form.deliveryType.value,
      address: sanitizeText(elements.form.address.value),
      tableNum: sanitizeText(elements.form.tableNum.value),
      specialInst: sanitizeText(elements.form.specialInst.value)
    };
  }

  function validateCustomerDetails(details) {
    if (details.fullName.length < 2) {
      return { field: elements.form.fullName, message: "Please enter your full name." };
    }
    if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/u.test(details.email)) {
      return { field: elements.form.email, message: "Please enter a valid email address." };
    }
    if (!/^\+?[0-9][0-9\s-]{7,18}$/u.test(details.phone)) {
      return { field: elements.form.phone, message: "Please enter a valid phone number." };
    }
    if (details.deliveryType === "delivery" && details.address.length < 10) {
      return { field: elements.form.address, message: "Please provide a complete delivery address." };
    }
    if (details.deliveryType === "dine" && details.tableNum && details.tableNum.length < 2) {
      return { field: elements.form.tableNum, message: "Please enter a valid table number or leave it blank." };
    }
    return null;
  }

  function validatePaymentDetails() {
    if (!isOnlinePaymentEnabled() && state.currentPayMethod !== "cod") {
      return { field: null, message: "Online payment is currently unavailable. Please choose Cash." };
    }

    switch (state.currentPayMethod) {
      case "card":
      case "upi":
      case "wallet":
        return null;
      default:
        return null;
    }
  }

  function sanitizeText(value) {
    return String(value || "").replace(/\s+/g, " ").trim();
  }

  function escapeHtml(value) {
    return String(value || "")
      .replace(/&/g, "&amp;")
      .replace(/</g, "&lt;")
      .replace(/>/g, "&gt;")
      .replace(/"/g, "&quot;")
      .replace(/'/g, "&#39;");
  }

  function escapeAttribute(value) {
    return escapeHtml(value).replace(/`/g, "&#96;");
  }

  function sanitizePhoneValue(value) {
    return String(value || "").replace(/[^\d+\-\s]/g, "").trim();
  }

  function sanitizePhone(event) {
    event.target.value = sanitizePhoneValue(event.target.value);
  }

  function digitsOnly(event) {
    event.target.value = event.target.value.replace(/\D/g, "");
  }

  function formatCardNumber() {
    const digits = elements.form.cardNumber.value.replace(/\D/g, "").slice(0, 19);
    elements.form.cardNumber.value = digits.replace(/(.{4})/g, "$1 ").trim();
    elements.cardBrand.textContent = detectCardBrand(digits);
  }

  function detectCardBrand(digits) {
    if (/^4/.test(digits)) {
      return "Visa";
    }
    if (/^(5[1-5]|2[2-7])/.test(digits)) {
      return "Mastercard";
    }
    if (/^3[47]/.test(digits)) {
      return "Amex";
    }
    return "Card";
  }

  function formatCardExpiry() {
    const digits = elements.form.cardExpiry.value.replace(/\D/g, "").slice(0, 4);
    if (digits.length >= 3) {
      elements.form.cardExpiry.value = `${digits.slice(0, 2)} / ${digits.slice(2)}`;
    } else {
      elements.form.cardExpiry.value = digits;
    }
  }

  function isValidExpiry(value) {
    const match = /^(\d{2})\s\/\s(\d{2})$/.exec(value);
    if (!match) {
      return false;
    }
    const month = Number(match[1]);
    const year = Number(`20${match[2]}`);
    if (month < 1 || month > 12) {
      return false;
    }
    const now = new Date();
    const expiry = new Date(year, month);
    return expiry > now;
  }

  function passesLuhn(number) {
    let sum = 0;
    let shouldDouble = false;
    for (let index = number.length - 1; index >= 0; index -= 1) {
      let digit = Number(number.charAt(index));
      if (shouldDouble) {
        digit *= 2;
        if (digit > 9) {
          digit -= 9;
        }
      }
      sum += digit;
      shouldDouble = !shouldDouble;
    }
    return sum % 10 === 0;
  }

  function clearSensitivePaymentFields() {
    elements.form.cardNumber.value = "";
    elements.form.cardExpiry.value = "";
    elements.form.cardCvv.value = "";
    elements.form.upiId.value = "";
    elements.cardBrand.textContent = "Card";
  }

  function resetCheckoutForm() {
    clearValidation();
    clearSensitivePaymentFields();
    elements.form.cardName.value = "";
    elements.form.fullName.value = "";
    elements.form.email.value = "";
    elements.form.phone.value = "";
    elements.form.deliveryType.value = "dine";
    elements.form.address.value = "";
    elements.form.tableNum.value = "";
    elements.form.specialInst.value = "";
    state.selectedUpiApp = "";
    state.selectedWallet = "";
    elements.upiApps.forEach((button) => button.classList.remove("selected"));
    elements.walletApps.forEach((button) => button.classList.remove("selected"));
    updateDeliveryVisibility();
    selectPayMethod("card");
    showStep(1);
  }

  function clearValidation() {
    Object.values(elements.form).forEach((field) => {
      if (field instanceof HTMLElement && "setAttribute" in field) {
        clearInvalid(field);
      }
    });
  }

  function markInvalid(field) {
    if (field && typeof field.focus === "function") {
      field.setAttribute("aria-invalid", "true");
      field.focus();
    }
  }

  function clearInvalid(field) {
    if (field && field.removeAttribute) {
      field.removeAttribute("aria-invalid");
    }
  }

  function formatMoney(value) {
    return new Intl.NumberFormat("en-US", {
      style: "currency",
      currency: state.publicConfig.payment.currency || CONFIG.currency,
      minimumFractionDigits: 2
    }).format(value);
  }

  function readableDeliveryType(value) {
    if (value === "delivery") {
      return "Home Delivery";
    }
    if (value === "takeaway") {
      return "Takeaway";
    }
    return "Dine In";
  }

  function generateOrderId() {
    if (window.crypto && typeof window.crypto.randomUUID === "function") {
      return `BG-${window.crypto.randomUUID().slice(0, 8).toUpperCase()}`;
    }
    return `BG-${Math.random().toString(36).slice(2, 10).toUpperCase()}`;
  }

  async function submitOrder() {
    const response = await fetch(`${CONFIG.apiBaseUrl}/api/orders`, {
      method: "POST",
      headers: {
        "Content-Type": "application/json"
      },
      body: JSON.stringify({
        customer: {
          fullName: elements.form.fullName.value.trim(),
          email: elements.form.email.value.trim(),
          phone: elements.form.phone.value.trim(),
          deliveryType: elements.form.deliveryType.value,
          address: elements.form.address.value.trim(),
          tableNumber: elements.form.tableNum.value.trim(),
          specialInstructions: elements.form.specialInst.value.trim()
        },
        items: state.cart.map((item) => ({
          menuItemId: item.id,
          quantity: item.qty
        })),
        paymentMethod: state.currentPayMethod === "cod" ? "cod" : "online",
        currency: state.publicConfig.payment.currency || CONFIG.currency
      })
    });

    const payload = await response.json().catch(() => ({}));
    if (!response.ok) {
      throw new Error(payload.error || "Backend order creation failed.");
    }

    return payload;
  }

  async function openRazorpayCheckout(orderResponse) {
    await loadScript("https://checkout.razorpay.com/v1/checkout.js");
    if (typeof window.Razorpay !== "function") {
      throw new Error("Razorpay checkout could not be loaded.");
    }

    const gateway = orderResponse.paymentGateway;
    const razorpay = new window.Razorpay({
      key: gateway.publishableKey,
      amount: Math.round(Number(gateway.amount) * 100),
      currency: gateway.currency,
      name: "Bhai G Cafe",
      description: `Order ${orderResponse.orderId}`,
      order_id: gateway.providerOrderId,
      prefill: {
        name: gateway.customerName,
        email: gateway.customerEmail,
        contact: gateway.customerPhone
      },
      theme: {
        color: "#c4622d"
      },
      async handler(paymentResult) {
        try {
          await verifyRazorpayPayment(orderResponse.orderId, paymentResult);
          finalizeSuccessfulOrder(orderResponse.orderId);
        } catch (error) {
          showStep(3);
          showToast(error && error.message ? error.message : "Payment was captured but backend verification failed.");
        }
      }
    });

    razorpay.on("payment.failed", function () {
      showStep(3);
      showToast("Payment failed or was cancelled.");
    });

    razorpay.open();
  }

  async function verifyRazorpayPayment(publicOrderId, paymentResult) {
    const response = await fetch(`${CONFIG.apiBaseUrl}/api/payments/razorpay/verify`, {
      method: "POST",
      headers: {
        "Content-Type": "application/json"
      },
      body: JSON.stringify({
        publicOrderId,
        providerOrderId: paymentResult.razorpay_order_id,
        providerPaymentId: paymentResult.razorpay_payment_id,
        signature: paymentResult.razorpay_signature
      })
    });

    const payload = await response.json().catch(() => ({}));
    if (!response.ok) {
      throw new Error(payload.error || "Backend payment verification failed.");
    }
  }

  function finalizeSuccessfulOrder(orderId) {
    elements.orderIdDisplay.textContent = `Order ID: ${orderId || generateOrderId()}`;
    elements.successNote.textContent = buildSuccessNote();
    state.cart = [];
    persistCart();
    renderCart();
    clearSensitivePaymentFields();
    showStep("success");
  }

  function loadScript(src) {
    return new Promise((resolve, reject) => {
      const existing = document.querySelector(`script[src="${src}"]`);
      if (existing) {
        resolve();
        return;
      }

      const script = document.createElement("script");
      script.src = src;
      script.async = true;
      script.onload = () => resolve();
      script.onerror = () => reject(new Error("Could not load payment gateway script."));
      document.head.appendChild(script);
    });
  }

  function showToast(message) {
    window.clearTimeout(toastTimer);
    elements.toast.textContent = message;
    elements.toast.classList.add("show");
    toastTimer = window.setTimeout(() => {
      elements.toast.classList.remove("show");
    }, 2500);
  }

  function updatePaymentMethodAvailability() {
    const onlineEnabled = isOnlinePaymentEnabled();
    elements.payMethodButtons.forEach((button) => {
      const method = button.dataset.method || "";
      button.disabled = method !== "cod" && !onlineEnabled;
      button.setAttribute("aria-disabled", String(button.disabled));
      if (button.disabled) {
        button.classList.remove("active");
      }
    });

    if (!onlineEnabled && state.currentPayMethod !== "cod") {
      state.currentPayMethod = "cod";
    }

    if (elements.paymentConfigNote) {
      elements.paymentConfigNote.textContent = onlineEnabled
        ? `${state.publicConfig.payment.provider} online checkout is enabled. Card, UPI, and wallet payments will be collected in the hosted payment popup.`
        : "Online payment is currently disabled on the server. Add Razorpay keys in server/appsettings.Local.json to enable Card, UPI, and wallet checkout.";
    }

    if (elements.cardPanelHelp) {
      elements.cardPanelHelp.textContent = onlineEnabled
        ? "Hosted checkout will securely collect live card details after you review the order. Local fields are never sent to your server."
        : "Online payment is disabled right now, so these demo fields stay inactive until Razorpay is configured on the server.";
    }

    if (!onlineEnabled) {
      state.selectedUpiApp = "";
      state.selectedWallet = "";
      elements.upiApps.forEach((button) => button.classList.remove("selected"));
      elements.walletApps.forEach((button) => button.classList.remove("selected"));
    }
  }

  function isOnlinePaymentEnabled() {
    return Boolean(state.publicConfig && state.publicConfig.payment && state.publicConfig.payment.onlineEnabled);
  }

  function buildSuccessNote() {
    const channels = [];
    if (state.publicConfig.notifications.emailEnabled) {
      channels.push("email");
    }
    if (state.publicConfig.notifications.smsEnabled) {
      channels.push("SMS");
    }

    if (!channels.length) {
      return "Order was saved successfully. Email and SMS confirmations will appear here after notification providers are configured.";
    }

    return `Order was saved successfully. Confirmation updates will be sent by ${channels.join(" and ")}.`;
  }
})();
