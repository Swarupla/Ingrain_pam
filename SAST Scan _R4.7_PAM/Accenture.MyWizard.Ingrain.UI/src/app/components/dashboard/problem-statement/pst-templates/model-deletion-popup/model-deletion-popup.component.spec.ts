import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { ModelDeletionPopupComponent } from './model-deletion-popup.component';

describe('ModelDeletionPopupComponent', () => {
  let component: ModelDeletionPopupComponent;
  let fixture: ComponentFixture<ModelDeletionPopupComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ ModelDeletionPopupComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(ModelDeletionPopupComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
